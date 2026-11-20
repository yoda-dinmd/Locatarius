using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Issues;

// Shape matches frontend/src/features/issue-triage/issueModel.ts (enums serialize as numbers).
public sealed record IssueDto(
    Guid IssueId, Guid ReportedBy, Guid BuildingId, string BuildingNumber,
    string Title, string Description, IssueStatus Status, IssuePriority Priority,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? ResolvedAt,
    IssueReporterDto Reporter, List<IssueAttachmentDto> Attachments);

public sealed record IssueReporterDto(Guid UserId, string FirstName, string LastName, IssueApartmentDto? Apartment);

public sealed record IssueApartmentDto(string ApartmentNumber);

public sealed record IssueAttachmentDto(
    Guid AttachmentId, Guid IssueId, Guid UploadedBy, string FileUrl, string FileType, DateTimeOffset CreatedAt);

public sealed record CreateIssueCommand(string Title, string Description, IssuePriority Priority);

public sealed record UpdateIssueCommand(IssueStatus? Status, IssuePriority? Priority);

public enum CreateIssueStatus { Created, ApartmentNotAssigned }
public sealed record CreateIssueResult(CreateIssueStatus Status, IssueDto? Issue = null);

public enum UpdateIssueStatus { Updated, NotFound, ResolvedPriorityLocked }
public sealed record UpdateIssueResult(UpdateIssueStatus Status, IssueDto? Issue = null);

/// <summary>
/// Issue reads and state transitions. Every query is scoped to the caller:
/// administrators see issues of buildings they administer, residents see issues
/// of the building that contains their assigned apartment. Out-of-scope issues
/// are indistinguishable from missing ones.
/// </summary>
public sealed class IssueService(LocatariusDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<List<IssueDto>> ListAsync(User caller, CancellationToken cancellationToken)
    {
        var scoped = await ScopeAsync(caller, cancellationToken);
        return await Project(scoped
                .OrderByDescending(issue => issue.CreatedAt).ThenByDescending(issue => issue.IssueId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IssueDto?> GetAsync(User caller, Guid issueId, CancellationToken cancellationToken)
    {
        var scoped = await ScopeAsync(caller, cancellationToken);
        return await Project(scoped.Where(issue => issue.IssueId == issueId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CreateIssueResult> CreateAsync(
        User resident, CreateIssueCommand command, CancellationToken cancellationToken)
    {
        var buildingId = await ResidentBuildingIdAsync(resident, cancellationToken);
        if (buildingId is null) return new(CreateIssueStatus.ApartmentNotAssigned);

        var issue = new Issue
        {
            IssueId = Guid.NewGuid(),
            // Reporter and building always come from the authenticated session, never from the request.
            ReportedBy = resident.UserId,
            BuildingId = buildingId.Value,
            Title = command.Title,
            Description = command.Description,
            Status = IssueStatus.Open,
            Priority = command.Priority,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.Issues.Add(issue);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = await Project(dbContext.Issues.AsNoTracking().Where(i => i.IssueId == issue.IssueId))
            .SingleAsync(cancellationToken);
        return new(CreateIssueStatus.Created, dto);
    }

    public async Task<UpdateIssueResult> UpdateAsync(
        Guid adminId, Guid issueId, UpdateIssueCommand command, CancellationToken cancellationToken)
    {
        // Ownership is part of the lookup predicate, so a foreign issue is never loaded or modified.
        var issue = await dbContext.Issues
            .SingleOrDefaultAsync(i => i.IssueId == issueId && i.Building.AdminId == adminId, cancellationToken);
        if (issue is null) return new(UpdateIssueStatus.NotFound);

        var targetStatus = command.Status ?? issue.Status;
        var priorityChanges = command.Priority is { } priority && priority != issue.Priority;
        if (priorityChanges && targetStatus == IssueStatus.Resolved)
            return new(UpdateIssueStatus.ResolvedPriorityLocked);

        var now = timeProvider.GetUtcNow();
        var changed = false;
        if (targetStatus != issue.Status)
        {
            issue.Status = targetStatus;
            // Resolving stamps ResolvedAt; reopening (Open or InProgress) clears it.
            issue.ResolvedAt = targetStatus == IssueStatus.Resolved ? now : null;
            changed = true;
        }
        if (priorityChanges)
        {
            issue.Priority = command.Priority!.Value;
            changed = true;
        }
        if (changed)
        {
            issue.UpdatedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var dto = await Project(dbContext.Issues.AsNoTracking().Where(i => i.IssueId == issueId))
            .SingleAsync(cancellationToken);
        return new(UpdateIssueStatus.Updated, dto);
    }

    private async Task<IQueryable<Issue>> ScopeAsync(User caller, CancellationToken cancellationToken)
    {
        var issues = dbContext.Issues.AsNoTracking();
        if (caller.Role?.Role == UserRoleType.Admin)
            return issues.Where(issue => issue.Building.AdminId == caller.UserId);
        if (caller.Role?.Role != UserRoleType.Resident)
            return issues.Where(_ => false);

        var buildingId = await ResidentBuildingIdAsync(caller, cancellationToken);
        return buildingId is null
            ? issues.Where(_ => false)
            : issues.Where(issue => issue.BuildingId == buildingId.Value);
    }

    private async Task<Guid?> ResidentBuildingIdAsync(User resident, CancellationToken cancellationToken)
    {
        if (resident.ApartmentId is not { } apartmentId) return null;
        return await dbContext.Apartments.AsNoTracking()
            .Where(apartment => apartment.ApartmentId == apartmentId)
            .Select(apartment => (Guid?)apartment.BuildingId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<IssueDto> Project(IQueryable<Issue> issues)
        => issues.Select(issue => new IssueDto(
            issue.IssueId, issue.ReportedBy, issue.BuildingId, issue.Building.BuildingNumber,
            issue.Title, issue.Description, issue.Status, issue.Priority,
            issue.CreatedAt, issue.UpdatedAt, issue.ResolvedAt,
            new IssueReporterDto(
                issue.Reporter.UserId, issue.Reporter.FirstName, issue.Reporter.LastName,
                issue.Reporter.Apartment == null ? null : new IssueApartmentDto(issue.Reporter.Apartment.ApartmentNumber)),
            issue.Attachments
                .OrderBy(attachment => attachment.CreatedAt).ThenBy(attachment => attachment.AttachmentId)
                .Select(attachment => new IssueAttachmentDto(
                    attachment.AttachmentId, attachment.IssueId, attachment.UploadedBy,
                    attachment.FileUrl, attachment.FileType, attachment.CreatedAt))
                .ToList()));
}
