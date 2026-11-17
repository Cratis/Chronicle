<!-- cratis-ai-managed: skills/cratis-arc-mcp/references/troubleshooting.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# MCP troubleshooting tables

## Contents

- Startup errors
- HTTP errors
- Tool errors
- Empty or incomplete tool list

## Startup errors

Validation runs in order; fix the first error.

| Exception or message | Cause | Fix |
|---|---|---|
| `McpRequiresCapabilities` | `WithMcp` without capability hosting | Add `WithCapabilities(...)` |
| `Capability endpoint group '<group>' has no port` | No port for the capabilities group | `EndpointGroups.Assign("capabilities", port)` |
| Health or readiness probe on the capabilities port is rejected | `GatedEndpoints` unset gates the whole port | Move probes off the capabilities port, or set an explicit `GatedEndpoints` selector (`WithMcp` adds `/mcp`) |
| `McpOptionsInvalid` | An option is invalid for the profile | OAuth profiles: `PublicOrigin`, `Resource` = `PublicOrigin` + `Path`, `AuthorizationServers`. Also bad `WriteScopes` |
| `McpToolNameInvalid` | Name outside `^[a-z][a-z0-9_]{0,63}$` | Lowercase letters, digits, underscores, start with a letter |
| `McpToolNameDuplicated` | Duplicate name | Rename one |
| `McpToolDescriptionMissing` | No description | Add `Description` or the builder argument |
| `McpExposedOperationUnknown` | Wrong `ExposeQuery<T>` method name, or the command has no handler | `nameof(ReadModel.Method)`; add `[Command]` and `Handle` |
| `McpApprovalRequiredCommandExposed` | Command has `[RequiresApproval]` | Do not expose it |
| `McpToolHasNoScope`, `McpToolScopeBlank` | No application name or explicit scope; blank scope | Set `ApplicationName`, or `ReadScope` and `WriteScope` |
| `McpWriteToolLacksWriteScope` | A write tool (usually curated) needs only read scopes | Require `WriteScope` or a `WriteScopes` entry |
| `McpToolNameTakenByServerTool` | Name clash with a tool registered on the MCP server | Rename one |
| `McpToolOutputSchemaInvalid` | Curated `OutputSchema` is not a JSON object schema | Fix the schema |
| Startup fails for a command you did not expose | Authorization is validated for every discovered command and query | Fix the missing or invalid policy it names |

Warning event 5103 at startup: `MCP requires scope '<scope>', which
PatOptions.AllowedScopes does not allow. PATs cannot satisfy this tool scope.`
It is logged once per missing scope, and only for a tool that no scope set in
`AllowedScopes` satisfies (required scopes and every alternative set are
checked). A tool a PAT can already satisfy raises no warning. Startup still
succeeds; no PAT can call that tool. Fix: add the scope to
`PatOptions.AllowedScopes`, or adjust `ReadScope`/`WriteScope` and the tool's
scope declarations. A scope with an `admin` or `approve` segment can never be
allowed on a PAT. Without a PAT installation the check does not run.

`PatOptions` with a resource key other than `direct`, `studio` or `stage-<app>`
(for example `local`) throws `ArgumentException` ("Invalid PAT resource key.").

## HTTP errors

| Status and body | Cause | Fix |
|---|---|---|
| 403 `origin_not_allowed` | `Origin` not `PublicOrigin` or in `AllowedOrigins` | Add the exact origin; Debug log of `McpAdmission` shows it |
| 401 `unauthorized` | No caller, or a credential kind the profile refuses. `WWW-Authenticate` has the realm (`PatOnly`) or the metadata URL, no scopes | Send `Authorization: Bearer <PAT>`; check expiry and revocation |
| 401 for a PAT that looks right | Token holds a scope not in `AllowedScopes` | Add every MCP scope, issue a new token |
| 403 `insufficient_scope` | Tool needs a scope the caller lacks | Issue a token with the scopes in `WWW-Authenticate` (it lists all scopes the tool requires, not only the missing ones) |
| 403 `forbidden` | An `IMcpToolAuthorizer` denied the call | Check the authorizer rule |
| 404 `not_found` on the well-known URL | Profile is `PatOnly` | Use `OAuthOnly` or `Combined` |
| 405 `method_not_allowed` | GET or DELETE on the path | POST only |
| 413 `request_too_large` | Body over `MaxRequestBodySize` (1 MiB) | Send less, or raise the option |
| 400 `invalid_request` | Invalid JSON, duplicate members, JSON-RPC batch | One message per request |
| Refused on the default port | MCP is served only on the capabilities port | Use that port and bind a listener on it |

## Tool errors

A tool failure is a result with `isError`, not an HTTP error.

| Text | Cause | Fix |
|---|---|---|
| `The arguments are not valid for this tool.` | Missing, mistyped or undeclared argument | Match `inputSchema` |
| `The caller lacks a scope this tool requires.` | Scope check failed at the tool | Issue a token with the scopes |
| `The caller is not allowed to call this tool.` | Authorizer or operation authorization refused | Check authorization |
| `The tool failed.` | The operation threw | Read the server log |
| `The tool is not available on this host.` | Streaming query, or a replaced command or query pipeline | Expose a non-streaming query; use Arc's pipelines |
| Result with `isValid: false` | Command validation failed | Read `validationResults` |

## Empty or incomplete tool list

A request with no accepted caller is refused with 401 before any list. For an
admitted caller, `tools/list` shows only tools its scopes and authorizers
allow. An empty list has three possible causes:

1. The token holds none of the exposed tools' scopes.
2. Authorizers hide every tool.
3. No tools are exposed.

A missing command tool means the token lacks the write scope or an
`IMcpToolAuthorizer` denies it.
