import assert from 'node:assert/strict';
import test from 'node:test';
import fixtures from './issuesMock.json' with { type: 'json' };
import { moveIssue, reprioritizeIssue } from './issueModel.ts';

const now = '2026-09-22T12:00:00.000Z';
const later = '2026-09-22T13:00:00.000Z';

function isUuid(value) {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

test('mock issues follow implemented issue fields and relationships', () => {
  assert.equal(fixtures.length, 5);
  assert.equal(new Set(fixtures.map(issue => issue.issueId)).size, 5);
  assert.equal(new Set(fixtures.map(issue => issue.buildingId)).size, 1);
  for (const issue of fixtures) {
    assert.ok(isUuid(issue.issueId));
    assert.ok(isUuid(issue.reportedBy));
    assert.ok(isUuid(issue.buildingId));
    assert.equal(issue.reporter.userId, issue.reportedBy);
    assert.ok([1, 2, 3].includes(issue.status));
    assert.ok([1, 2, 3, 4].includes(issue.priority));
    assert.equal(issue.resolvedAt !== null, issue.status === 3);
    assert.equal('category' in issue || 'note' in issue || 'location' in issue || 'reference' in issue, false);
    for (const attachment of issue.attachments) {
      assert.ok(isUuid(attachment.attachmentId));
      assert.equal(attachment.issueId, issue.issueId);
      assert.equal(attachment.uploadedBy, issue.reportedBy);
      assert.ok(attachment.fileUrl.startsWith('/issue-demo/'));
    }
  }
});

test('moving to Closed sets resolution and reopening clears it', () => {
  const original = structuredClone(fixtures);
  const id = fixtures[0].issueId;
  const started = moveIssue(fixtures, id, 2, now);
  assert.equal(started[0].status, 2);
  assert.equal(started[0].updatedAt, now);
  assert.equal(started[0].resolvedAt, null);
  const closed = moveIssue(started, id, 3, later);
  assert.equal(closed[0].status, 3);
  assert.equal(closed[0].resolvedAt, later);
  const reopened = moveIssue(closed, id, 2, now);
  assert.equal(reopened[0].status, 2);
  assert.equal(reopened[0].resolvedAt, null);
  assert.equal(reopened[0].updatedAt, now);
  assert.deepEqual(reopened.slice(1), fixtures.slice(1));
  assert.deepEqual(fixtures, original);
});

test('dropping onto the same status or an unknown issue makes no change', () => {
  assert.deepEqual(moveIssue(fixtures, fixtures[0].issueId, 1, now), fixtures);
  assert.deepEqual(moveIssue(fixtures, 'missing', 3, now), fixtures);
});

test('reprioritization changes only an active issue and preserves closed history', () => {
  const changed = reprioritizeIssue(fixtures, fixtures[0].issueId, 4, now);
  assert.equal(changed[0].priority, 4);
  assert.equal(changed[0].updatedAt, now);
  assert.deepEqual(changed.slice(1), fixtures.slice(1));
  assert.deepEqual(reprioritizeIssue(fixtures, fixtures[4].issueId, 4, now), fixtures);
  assert.deepEqual(reprioritizeIssue(fixtures, fixtures[0].issueId, 3, now), fixtures);
});
