---
name: cratis-arc-mcp
description: Use when exposing Arc commands or queries to AI agents as MCP tools, naming, describing and scoping those tools, or connecting Claude, VS Code or MCP Inspector with a personal access token. Not for Chronicle's own MCP server or authoring the command itself (cratis-arc-command).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-mcp/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Expose Arc commands and queries over MCP

`Cratis.Arc.Mcp` serves an MCP endpoint from an Arc application. An agent calls
the commands and queries you declare, as the verified caller the capability gate
admitted. Nothing is exposed unless declared, and the tool set is frozen at
startup.

Requires: the `Cratis.Arc.Mcp` package, capability hosting, and a verified-caller
installation (`WithPersonalAccessTokens(...)` or `WithVerifiedCallers(...)`).

## Decide what to expose

1. Expose queries first. A query tool is annotated read-only and idempotent.
2. Expose a command only when an agent acting for a user should be able to do
   what the command does. The command's own authorization still applies, but
   exposure is a product decision. Least privilege: the smallest set of tools,
   each with the narrowest scopes.
3. Never expose a command that needs approval (`[RequiresApproval]`, inherited).
   Startup fails with `McpApprovalRequiredCommandExposed`.
4. Streaming (observable) queries are not available as tools; a call returns
   "The tool is not available on this host."
5. Do not expose by default or by convention. A new operation stays unexposed
   until someone adds the declaration.

## Declare the tool

Default: the attribute, next to the code. It applies to a command class or a
read model query method, once, and is not inherited.

```csharp
using System.ComponentModel;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Mcp;
using Cratis.Arc.Queries.ModelBound;

[Command]
[ExposeOverMcp("add_note", Description = "Adds a note and returns its id.")]
public record AddNote([property: Description("The note text, plain text.")] string Text)
{
    public Guid Handle() => Guid.NewGuid();
}

[ReadModel]
public record NoteReadModel(string Text)
{
    [ExposeOverMcp("list_notes", Description = "Lists notes, newest first. Use page_size to limit the result.")]
    public static IQueryable<NoteReadModel> All() => Enumerable.Empty<NoteReadModel>().AsQueryable();
}
```

Use the builder only when the type cannot carry the attribute:
`mcp.Expose<AddNote>("add_note", "Adds a note.")` and
`mcp.ExposeQuery<NoteReadModel>(nameof(NoteReadModel.All), "list_notes", "Lists notes.")`.
The method name is checked at startup (`McpExposedOperationUnknown`); the string
overload `ExposeQuery("Ns.Type.Method", ...)` is obsolete, do not use it.

Only a command that has `[Command]` **and** a `Handle` method, and a read
model (`[ReadModel]`) query method, are discoverable. Without them the tool is
not found.

## Name and describe

- Name matches `^[a-z][a-z0-9_]{0,63}$` and is unique (`McpToolNameInvalid`,
  `McpToolNameDuplicated`). Names are explicit and never derived from the CLR
  name. Treat them as public API: clients and prompts store them. Renaming a
  class does not rename the tool; renaming the tool breaks clients.
- The tool description is required (`McpToolDescriptionMissing`). It is the only
  guidance the agent gets: one sentence on the effect, the inputs that must be
  right, what comes back. No secrets, internal names or instructions about
  other tools.
- Put `[Description("...")]` (`System.ComponentModel`) on every command
  property (`[property: Description]` on a record parameter) and every query
  method parameter. It becomes the argument's `description` in the input
  schema. Arc does not infer it from names or XML comments.
- Query tools take the query parameters as arguments; paging applies only to
  queries returning `IQueryable<T>`; those tools accept `page_size` (optional
  `page`). An `IEnumerable<T>` query has no paging arguments and supplying them
  is an invalid-arguments error. Argument values are scalars, or arrays
  for collection parameters.

## Scopes

| Tool | Always requires |
|---|---|
| Query | `ReadScope` |
| Command | `WriteScope` |

Defaults are `<ApplicationName>:read` and `<ApplicationName>:write`. With no
application name and no explicit scope, startup fails with `McpToolHasNoScope`.

- Add requirements with `[RequiresScope("notes:restricted-read")]`
  (`Cratis.Arc.Authorization`); repeatable, the caller must hold all of them.
  The attribute targets classes and methods only: put it on the command type or
  its handler method, and on the read model type, the performer type or the
  query method. It cannot go on a parameter.
- `WriteScopes` lists additional write scopes; `WriteScope` is always one. An
  entry must not be blank, repeated or equal to `ReadScope`
  (`McpOptionsInvalid`).
- `IMcpToolAuthorizer` (`AddToolAuthorizer<T>()`) handles rules a scope cannot
  express. It only narrows: it runs after scopes pass, filters `tools/list`
  and refuses calls (HTTP 403 `forbidden`). It cannot grant.
- Every scope a tool requires, including `[RequiresScope]` extras, must be in
  `PatOptions.AllowedScopes`, or no PAT can call the tool. The list is empty by
  default, case-sensitive, with no wildcards. A scope with an `admin` or
  `approve` segment (split on `:`, `.`, `/`, `-`) is never allowed on a PAT,
  so a tool that requires one cannot be called with a PAT at all. Use a name
  such as `notes:restricted-read`, not `notes:admin-read`.
- Startup logs warning event 5103 (`MCP requires scope '<scope>', which
  PatOptions.AllowedScopes does not allow. PATs cannot satisfy this tool
  scope.`) once per missing scope, only for a tool that no scope set in
  `AllowedScopes` satisfies (required scopes and every alternative set are
  checked). Startup still succeeds and no PAT can call that tool. Fix: add the
  scope to `AllowedScopes`, or change the tool's scopes. Without a PAT
  installation the check does not run.

## Host setup (summary)

Choose the verified-caller installation first:

- **PAT-only** (default for MCP): `WithPersonalAccessTokens(options, callers => callers.ForResource(...).UsePersonDirectory<T>())`.
  No forwarding configuration and no `AllowPersonalAccessTokenCallers()`;
  adding forwarding here throws `InvalidVerifiedCallerInstallation`.
- **PAT plus signed browser issuance**: add `UseSignedForwardedIdentityV2(...)`
  **and** `AllowPersonalAccessTokenCallers()`. The opt-in without forwarding
  configuration throws `InvalidVerifiedCallerInstallation`. Use this only when
  the same host issues PATs to signed browser sessions; see the reference.

PAT-only installation:

```csharp
builder.AddCratisArc(
    options => options.EndpointGroups.Assign("capabilities", capabilityPort),
    configureBuilder: arc => arc
        .WithCapabilities()
        .WithPersonalAccessTokens(
            new PatOptions("stage-notes") { AllowedScopes = ["notes:read", "notes:write"] },
            callers => callers
                .ForResource("https://notes.example.test", "notes", "production")
                .UsePersonDirectory<PersonDirectory>())
        .WithMcp(mcp => mcp.Configure(o =>
        {
            o.ApplicationName = "notes";
            o.PublicOrigin = "https://notes.example.test";
        })));

var app = builder.Build();
app.UseCratisArc();
app.MapArcMcp();
```

Read [references/setup-and-profiles.md](references/setup-and-profiles.md) for
the full prerequisites (ports and listeners, gating, verified
callers), the credential profiles, `Origin` and `AllowedOrigins`, and the
development PAT adapters.

Rules to keep in mind:

- The `PatOptions` resource key must be `direct`, `studio` or `stage-<app>`
  (`<app>`: lowercase letters, digits and hyphens, 1 to 48 characters, starting
  and ending with a letter or digit). Any other key, such as `local` or
  `notes`, throws `ArgumentException`.
- Use `WithCapabilities()` without a `GatedEndpoints` selector: the whole
  capabilities port is then gated after routing, including health and readiness
  probes (only OAuth resource metadata stays anonymous). Selective gating is an
  explicit opt-out: set `CapabilitiesOptions.GatedEndpoints` to a selector
  (`WithMcp` adds MCP to it) and keep other routes you need outside the gate.
- The endpoint is served only on the capabilities port. Bind a listener on it
  as well as the default listener.
- Default profile `PatOnly` suits CLI and IDE clients. Hosted connectors cannot
  be handed a PAT; they need `OAuthOnly` or `Combined` plus your own OAuth
  authorization server.
- `UseDevelopmentPersonalAccessTokenAdapters()` is Development-only: in-memory,
  lost on restart, host startup throws elsewhere. Never combine with
  production adapters. It is the only public development PAT type; the adapter
  classes behind it are internal, so never reference them. A production host
  implements the public `IPatAuthority`, `IPatPolicy` and `IPatRecoveryEpoch`.
- Keep `ExclusiveTools = true` (default). Turning it off serves tools outside
  the ceiling with no scope checks.
- Keep `AllowedOrigins` empty unless a browser client needs it.

## What the agent sees

- A request with no accepted caller, or a credential kind the profile refuses,
  gets HTTP 401 `unauthorized` before any tool list. Its `WWW-Authenticate`
  carries the realm (`PatOnly`) or the resource metadata URL, not scopes.
- `tools/list` shows only tools whose scopes the caller holds and every
  authorizer allows. A token with only the read scope never sees a command
  tool; a token with no MCP scopes gets an empty list.
- `inputSchema` is generated from the command properties or query parameters
  and rejects undeclared arguments. Query tools are annotated read-only and
  idempotent; command tools destructive and not idempotent.
- A result carries the same JSON as text and as `structuredContent` matching
  `outputSchema`: `outcome` (`isSuccess`, `isAuthorized`, `isValid`,
  `validationResults`) plus `response` (commands) or `data` and `paging`
  (queries). Validation failures and refusals are tool errors, not HTTP errors.
- A call outside the caller's scopes is HTTP 403 `insufficient_scope`; the
  `WWW-Authenticate` `scope` value lists all scopes the tool requires, not only
  the missing ones.
- Exceptions come back as the generic "The tool failed."; no message leaks.

## Audit log

Every `tools/call` writes one Information entry in the
`Cratis.Arc.Mcp.McpCallAudit` category (event 5102): tool, kind, person,
tenant, credential id and kind, outcome, correlation id. Never the token,
arguments or results. Outcomes: `succeeded`, `failed`, `insufficient_scope`,
`not_allowed`, `invalid_arguments`, `malformed`, `error`. Person and tenant are
personal data; set log retention accordingly. A request with no accepted
caller is not audited.

## Verify

1. Run the host. Startup validates every tool and throws an
   `InvalidMcpConfiguration` subclass for the first problem. A pass is a host
   that starts. Authorization declarations are validated for every command and
   query, not just exposed ones.
2. List tools: `curl -s <url> -H "Authorization: Bearer $PAT" -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'`.
   Pass: your tool names appear; a read-only token omits command tools.
3. Interactive check: `npx @modelcontextprotocol/inspector`, transport
   Streamable HTTP, URL `https://<host>:<capabilityPort>/mcp`, header
   `Authorization: Bearer <token>`. A browser Inspector sends an `Origin`; see
   the reference for `AllowedOrigins`. Remove the entry afterwards.
4. Specs: follow the host pattern in `Source/DotNET/Arc.Mcp.Specs` of the Arc
   repository (real host on the capabilities port, a substituted
   `IVerifiedCallerAccessor`). Cover a read-only caller, a caller missing a
   `[RequiresScope]`, and a denying `IMcpToolAuthorizer`.

## Common mistakes

| Mistake | Symptom |
|---|---|
| No `[Command]` or no `Handle` | `McpExposedOperationUnknown` |
| `ExposeQuery<T>` method name that does not exist | `McpExposedOperationUnknown` at startup |
| No description | `McpToolDescriptionMissing` |
| No `ApplicationName`, `ReadScope` or `WriteScope` | `McpToolHasNoScope` |
| MCP scopes missing from `PatOnly` `AllowedScopes` | 401 for a PAT that looks right; warning event 5103 at startup when no PAT can satisfy a tool |
| Probes left on the capabilities port with `GatedEndpoints` unset | Probes require a verified caller; move them or set an explicit selector |
| Listener not bound on the capabilities port | Not found on the default port, or no connection |
| Exposing a `[RequiresApproval]` command | `McpApprovalRequiredCommandExposed` |
| Browser client without `AllowedOrigins` | 403 `origin_not_allowed` |
| Hosted connector on `PatOnly` | 401, no metadata (404 on the well-known URL) |
| Development adapters in a non-Development host | Startup `InvalidOperationException` |
| Argument description missing | Agent guesses the argument |

Read [references/troubleshooting.md](references/troubleshooting.md) for the
complete error, HTTP status and tool error tables.

## Related skills

- `cratis-arc-command` for the command and its handler.
- `cratis-arc-query-paging` for query paging that `page` and `page_size` map to.
- `cratis-arc-hosting` for application hosting and `cratis-arc-authentication-authorization-and-identity` for authorization policies.
