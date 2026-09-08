# Primary sources and verification limits

Sources below were searched or opened during preparation on 7 September 2026. They support the specific technical claims identified, not the product's assumed business rules or legal compliance. Product versions, provider contract features, regions, package compatibility, license/support terms and exact prices must be verified again during implementation/procurement. No external source is invented or presented as evidence that this application has been tested.

| ID | Primary source | Use and verification boundary |
| --- | --- | --- |
| SRC-01 | [IETF RFC 9700 — OAuth 2.0 Security BCP](https://www.rfc-editor.org/info/rfc9700/) | Code/PKCE, redirect and token-security reasoning; opened primary text. Application-specific session durations are proposed policy |
| SRC-02 | [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html) | Identity issuer/subject and verification framework; primary source lookup. No selected provider compatibility claim |
| SRC-03 | [OpenID RP-Initiated Logout 1.0](https://openid.net/specs/openid-connect-rpinitiated-1_0.html) | Mechanism for requesting IdP logout; local app revocation remains independent |
| SRC-04 | [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0) | Cookie-authenticated request CSRF threat and middleware capability; primary documentation lookup |
| SRC-05 | [ASP.NET Core SameSite](https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-10.0) | Cookie/OIDC interaction; verify chosen middleware settings in browser integration tests |
| SRC-06 | [PostgreSQL row security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html) | RLS role bypass and FORCE semantics, integrity-check caveats; opened primary documentation |
| SRC-07 | [PostgreSQL range types](https://www.postgresql.org/docs/current/rangetypes.html) | Exclusion constraints can prohibit overlapping ranges; actual operator class/extension/version availability needs migration spike |
| SRC-08 | [PostgreSQL PITR and continuous archiving](https://www.postgresql.org/docs/current/continuous-archiving.html) | Backup/WAL restore mechanism; proposed RPO/RTO not guaranteed by document |
| SRC-09 | [Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | Support lifecycle selection; opened primary page. Exact current patch/package matrix deliberately unclaimed |
| SRC-10 | [Microsoft .NET 10 announcement](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/) | Confirms .NET 10 LTS status; recheck support when building |
| SRC-11 | [W3C WCAG 2.2](https://www.w3.org/TR/WCAG22/) | Accessibility technical criteria; opened recommendation, no conformance or legal certification claim |
| SRC-12 | [OWASP authorization guidance](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html) | Deny-by-default/action/resource authorization rationale; not a substitute for application testing |
| SRC-13 | [OWASP file upload guidance](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html) | Layered type/size/scan/storage defenses; scanner product not selected |
| SRC-14 | [Amazon S3 presigned access](https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html) | Time-limited reusable holder access explains authenticated download choice; no claim all S3-compatible stores behave identically |

The modular-monolith choice, release plan, effort ranges, financial policies, polling/retention/session defaults and capacity targets are design recommendations/inferences for the declared assumptions. They are not statements sourced from these standards. Jurisdiction-specific legal citations are intentionally absent until jurisdiction and organizational obligations are established.
