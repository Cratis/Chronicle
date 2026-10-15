---
applyTo: "**/.cratis/**"
paths:
  - "**/.cratis/**"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/profiles.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# AI Corpus Profiles

Profiles determine which rules and skills apply to your work. They define the scope of the AI corpus and which documentation, conventions, and skills are available.

## Two Main Profile Types

### Application Profile

**Use when:** You are building an application on Cratis.

**What it includes:**
- Event-sourced CQRS with **Cratis Chronicle** + **Cratis Arc**
- Vertical slices (commands, events, projections, read models)
- React + Cratis Components (PrimeReact) frontend in MVVM
- MongoDB or EF Core for read models
- Full-stack type safety from C# to TypeScript

**Key rules:**
- [vertical-slices.md](./vertical-slices.instructions.md) - slice anatomy and structure
- [react.md](./react.instructions.md) - React + Arc + Cratis Components
- [components.md](./components.instructions.md) - component structure and styling
- [dialogs.md](./dialogs.instructions.md) - dialog patterns
- [specs.scenarios.csharp.md](./specs.scenarios.csharp.instructions.md) - in-process scenario family

### Framework Profile

**Use when:** You are contributing to a Cratis framework repository itself (Arc, Chronicle, Fundamentals, Components, Specifications).

**What it includes:**
- Library development (not applications)
- Source generators, the Chronicle kernel (Orleans grains + storage), client SDKs, React component library
- No vertical slices, no model-bound `[Command]`/`[ReadModel]` artifacts
- No projections/read-models, no MVVM app components

**Key rules:**
- [framework.md](./framework.instructions.md) - repo structure and library/API design
- [orleans.md](./orleans.instructions.md) - Orleans grain conventions
- [specs.csharp.md](./specs.csharp.instructions.md) - universal `Specification` base + NSubstitute

## Profile Catalog

The complete list of available profiles is defined in [profile-catalog.json](../../../profile-catalog.json). This catalog includes:

### Application Profiles

| Profile ID | Description | Automatically Includes |
|---|---|---|
| `cratis/application` | Full application stack (C# + React + TypeScript) | `cratis/application/csharp`, `cratis/application/elixir`, `cratis/application/java`, `cratis/application/kotlin`, `cratis/application/typescript`, `cratis/arc/core`, `cratis/arc/react`, `cratis/chronicle/core`, `cratis/components`, `cratis/fundamentals`, `cratis/specifications/dotnet`, `cratis/specifications/typescript` |
| `cratis/application/csharp` | C# backend with Arc + Chronicle | `cratis/arc`, `cratis/arc/react`, `cratis/chronicle`, `cratis/components`, `cratis/fundamentals`, `cratis/language/csharp`, `cratis/screenplay`, `cratis/specifications/dotnet`, `cratis/specifications/typescript` |
| `cratis/application/react` | React frontend with Cratis Components | `cratis/arc/core`, `cratis/arc/react`, `cratis/components`, `cratis/fundamentals`, `cratis/specifications/dotnet`, `cratis/specifications/typescript` |
| `cratis/application/typescript` | TypeScript client for Chronicle | `cratis/chronicle/client-typescript`, `cratis/language/typescript`, `cratis/screenplay`, `cratis/specifications/typescript` |
| `cratis/application/arc-chronicle` | Arc + Chronicle integration | `cratis/arc/core`, `cratis/chronicle/core`, `cratis/fundamentals`, `cratis/screenplay`, `cratis/specifications/dotnet` |
| `cratis/application/arc-only` | Arc without Chronicle | `cratis/arc/core`, `cratis/fundamentals`, `cratis/screenplay`, `cratis/specifications/dotnet` |
| `cratis/application/chronicle-dotnet` | Chronicle .NET client | `cratis/chronicle/client-dotnet`, `cratis/chronicle/core`, `cratis/fundamentals`, `cratis/screenplay`, `cratis/specifications/dotnet` |
| `cratis/application/elixir` | Elixir Chronicle client | `cratis/chronicle/client-elixir`, `cratis/language/elixir`, `cratis/screenplay` |
| `cratis/application/kotlin` | Kotlin Arc + Chronicle application | `cratis/arc/client-kotlin`, `cratis/chronicle/client-kotlin`, `cratis/language/kotlin`, `cratis/screenplay` |
| `cratis/application/java` | Java Arc + Chronicle application | `cratis/arc/client-kotlin`, `cratis/chronicle/client-java`, `cratis/language/java`, `cratis/screenplay` |

`cratis/application` inherits `cratis/screenplay` through its language-specific
children. Backend-bearing application profiles include the method skills and MCP
declaration, not `cratis/stage`; the frontend-only `cratis/application/react`
profile includes neither Screenplay nor Stage.

### Framework Profiles

| Profile ID | Description | Automatically Includes |
|---|---|---|
| `cratis/arc` | Arc CQRS framework | `cratis/arc/csharp`, `cratis/arc/java`, `cratis/arc/kotlin` |
| `cratis/chronicle` | Chronicle event sourcing engine (does not compose `cratis/chronicle/mcp`) | `cratis/chronicle/compliance`, `cratis/chronicle/csharp`, `cratis/chronicle/elixir`, `cratis/chronicle/java`, `cratis/chronicle/kotlin`, `cratis/chronicle/multi-tenancy`, `cratis/chronicle/typescript`, `cratis/chronicle/web-workbench` |
| `cratis/components` | React component library, including `cratis-components-common-controls` and `cratis-components-data-surfaces` | (no child profiles) |
| `cratis/fundamentals` | Core primitives and generated .NET telemetry (`cratis-fundamentals-telemetry`) | (no child profiles) |
| `cratis/arc/go` | Experimental Arc.Go HTTP CQRS (root module) | `cratis/language/go`, `cratis/fundamentals/go` |
| `cratis/chronicle/client-go` | Experimental standalone Chronicle.Go client | `cratis/language/go`, `cratis/fundamentals/go` |
| `cratis/fundamentals/go` | Fundamentals.Go named scalar concepts and codecs | `cratis/language/go` |
| `cratis/specifications` | Specification framework | (no child profiles) |
| `cratis/specifications/dotnet` | C# specifications and application slice conformance (`cratis-application-slice-conformance`), alongside slice specifications | `cratis/specifications` |

### Engineering Profiles

| Profile ID | Description | Automatically Includes |
|---|---|---|
| `cratis/engineering` | Engineering conventions and workflows | `cratis/engineering/core`, `cratis/engineering/csharp`, `cratis/engineering/elixir`, `cratis/engineering/go`, `cratis/engineering/kotlin`, `cratis/engineering/react`, `cratis/engineering/typescript` |
| `cratis/engineering/csharp` | C# engineering conventions and framework Orleans jobs (`cratis-orleans-jobs`, alongside `orleans.md`) | `cratis/engineering/core` |
| `cratis/engineering/go` | Go framework parity and immutable module releases | `cratis/engineering/core`, `cratis/language/go` |
| `cratis/engineering/typescript` | TypeScript engineering conventions | `cratis/engineering/core` |
| `cratis/engineering/react` | React engineering conventions | `cratis/engineering/core` |

### Language Profiles

| Profile ID | Description |
|---|---|
| `cratis/language/csharp` | C# language conventions |
| `cratis/language/typescript` | TypeScript language conventions |
| `cratis/language/elixir` | Elixir language conventions |
| `cratis/language/go` | Go conventions for source, go.mod and go.sum |
| `cratis/language/kotlin` | Kotlin language conventions |
| `cratis/language/java` | Java language conventions |

### Arc and Chronicle on the JVM (Kotlin and Java)

Arc.Kotlin (`io.cratis:arc`) and its optional Chronicle integration
(`io.cratis:cratis`, the all-in-one event-sourced application artifact, or
`io.cratis:arc-chronicle-spring-boot-starter`) bring the same command/query
model-bound shape to the JVM that Arc .NET brings to C#, for both Kotlin and
Java application code. `cratis/arc/client-kotlin` carries the skills for both
languages — Java application code still needs Kotlin and KSP on the build,
since Arc generates Kotlin adapters for Java declarations.

| Skill | Covers |
| --- | --- |
| `cratis-arc-command-kotlin` | `@Command`, `handle()`/`provide()`, Chronicle event responses, command authorization, TypeScript proxy generation |
| `cratis-arc-query-kotlin` | `@ReadModel` queries, GET vs RFC QUERY, observable queries (`Flow`, `Flow.Publisher`, RxJava 3) over SSE/WebSocket |
| `cratis-arc-validation-kotlin` | `FluentModelValidator` shared rules, `CommandValidator`/`QueryValidator`/`ConceptValidator`/`ModelValidator`, Jakarta constraints |
| `cratis-arc-testing-kotlin` | Real in-process command/query scenarios, bounded observable collection, read-model pins and Chronicle append assertions |
| `cratis-arc-identity-and-tenancy-kotlin` | Authentication handlers, identity details, tenant resolution and propagation, opt-in trusted platform headers |
| `cratis-arc-spring-data-kotlin` | JPA/Mongo repositories, paging, observations, concept converters, tenant storage and transaction boundaries |

Standalone Chronicle client usage (no Arc) for Kotlin and Java is
`cratis-chronicle-client-kotlin`, reused by `cratis/chronicle/client-java`.
JVM language conventions are `kotlin.md` and `java.md`.

### Arc for TypeScript on Node.js

Arc for TypeScript (`@cratis/arc.core` with the Express, Fastify, Hono,
MongoDB, Drizzle, testing and experimental Chronicle packages, plus
`@cratis/arc.proxygenerator`, `@cratis/eslint-plugin-arc-core` and experimental
`@cratis/cratis`) brings the same model-bound command/query shape to a Node.js
server. Verified at GitHub pre-release **v0.57.2**: its packages are a source
preview and are not published to npm. It is separate from `@cratis/arc`, the
published client runtime that generated proxies import in a frontend.
`cratis/arc/server-typescript` carries the skills; `cratis/arc/typescript` adds
the TypeScript language conventions, and `cratis/full/typescript` composes it.
The server skills are opt-in: `cratis/arc`, `cratis/application` and
`cratis/application/typescript` do not load them, because they target a
Node.js server and a C# or React-frontend project has no use for them. Select
`cratis/arc/typescript` (or `cratis/full/typescript`) for a Node.js Arc server.

| Skill | Covers |
| --- | --- |
| `cratis-arc-command-typescript` | Project setup from the source preview, `ArcApplication.createBuilder()`, `@command()` with `handle()`/`provide()`, outcomes, authorization, returned Chronicle events, generated metadata and proxies, hosts, `CommandScenario` |
| `cratis-arc-query-typescript` | `@readModel()` with static `@query()` methods, argument binding, GET and HTTP `QUERY`, paging, RxJS observable queries, MongoDB/Drizzle/Chronicle read models, `QueryScenario`/`ObservableQueryScenario` |
| `cratis-arc-validation-typescript` | `CommandValidator`/`QueryValidator`/`ConceptValidator`/`ModelValidator`, `readModelForValidation`, `validation()` with `rejected`/`denied`, severity filtering |

Standalone Chronicle client usage (no Arc) in TypeScript is
`cratis-chronicle-client-typescript`.

### Experimental Go modules

Select `cratis/language/go` for a Go repository without importing any HTTP or
Chronicle guidance. `cratis/engineering/go` adds contributor parity and release
policy through [go-engineering.md](./go-engineering.instructions.md), composing rather than
duplicating [go.md](./go.instructions.md). Set `languages: ["go"]` in `.cratis/ai.json` so
language-filtered composition selects the Go children. The catalog/manifest
register `go`; there is no separate extension-based detection registry in the
corpus's harness setup or Pi package.

| Profile | Skill | Verified experimental version |
| --- | --- | --- |
| `cratis/chronicle/client-go` | `cratis-chronicle-client-go` | Chronicle.Go v0.9.1 (Chronicle kernel 19.32.3 profile) |
| `cratis/arc/go` | `cratis-arc-go` | Arc.Go v0.1.1 |
| `cratis/fundamentals/go` | `cratis-fundamentals-concept-go` | Fundamentals.Go v0.3.2 |

Arc and Chronicle profiles compose language and Go concepts, **not each other**
or C# client/HTTP skills. These profiles are explicit opt-ins; existing aggregate
Arc/Chronicle and application profiles are unchanged. Only the Arc.Go root module
is released; Chronicle/MongoDB integrations and Go generator tools remain source
previews. `cratis/application/go` and Go scaffolding are deferred; do not prescribe
`dotnet new` as a Go template. Each module currently requires Go 1.26, but generic
language policy always follows the consuming repository's `go.mod` minimum.

### Specialized Profiles

| Profile ID | Description |
|---|---|
| `cratis/documentation` | Reader-centered product docs, technical examples, release notes, and voice review |
| `cratis/content` | Release notes, social feed posts, voice review, content length, and developer marketing and distribution |
| `cratis/review` | Code review, performance, security |
| `cratis/codescene` | Opt-in CodeScene Code Health change gates, refactoring, debt prioritization and risk-based testing; requires an account and available MCP tools; each user registers the MCP server (see below) |
| `cratis/studio` | Studio MCP safety guidance |
| `cratis/cli` | CLI operations |
| `cratis/cli/terminal-workbench` | Human-operated terminal Workbench |
| `cratis/chronicle/mcp` | Chronicle MCP inspection and exact tool-effect guidance |
| `cratis/lens` | Lens browser extension |
| `cratis/screenplay` | Model-first event modeling: lifecycle, phase skills, toolchain, review, render and gap-fill, plus the whole `.play` language |
| `cratis/stage` | Stage rendering and sandbox |
| `cratis/modeling/screenplay-stage` | Screenplay + Stage together |

### Code Health with CodeScene

Select `cratis/codescene` explicitly to load the four upstream CodeScene skills:
`safeguarding-ai-generated-code`, `guiding-refactoring-with-code-health`,
`prioritizing-technical-debt` and `risk-based-testing-with-code-health`.
The [CodeScene policy](../../../rules/codescene.md) overrides those skills where they differ.
No other profile, including `cratis` and `cratis/full`, composes it.

The profile selects the `codescene` stdio server (`cs-mcp`, no arguments) from
`mcp-servers.json`, but neither the Cratis CLI nor any harness registers it,
because it needs a per-user CodeScene account and a locally installed binary.
Each user registers it in their own user-level MCP configuration, never in
committed project files: `cs-mcp` over stdio (`brew install cs-mcp`), or
`npx -y @codescene/codehealth-mcp`.
CodeScene needs an account and OAuth `login`; without an account or available
CodeScene tools, skip its steps and say so rather than blocking contributors.
The MCP catalog has no install-hints field and does not install the executable.

### Event modeling with Screenplay

**Model first.** A repository is opted in only when its model root (where the project's
`.play` files live: normally under `Source/` or `src/`, else `Screenplay/`) holds a committed `.play` file (`git ls-tree -r --name-only HEAD` lists a `.play` file there, narrowed to `-- <root>` when a root is configured) or the project explicitly set
`mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install output, an
installed profile or skill, a `.play` file outside the root or an untracked or uncommitted draft is not
opt-in. An accepted model under the root covering the scope is the contract and the source
of truth for behavior. Change the model, verify and independently review it, then render
or gap-fill from its contract; never edit Stage-managed output or weaken protection.
Otherwise stay code-first; only the entry-point session may propose a model, at most once
per session. Master definition: `cratis-screenplay-modeling-lifecycle`. Framework and non-opted-in brownfield work remain
code-first; infrastructure, clients, adapters and code attachments stay in code.
Backend-bearing application profiles automatically compose `cratis/screenplay`;
installing its skills or registering its MCP server is not model-first opt-in.

`cratis/screenplay` carries the **method** and the **language**. Load the lifecycle
and one phase skill, then the construct skill needed for syntax.

#### Method

| Skill | Covers |
| --- | --- |
| `cratis-screenplay-event-modeling` | Entry point, model-first routing, four slice types and nine-step map |
| `cratis-screenplay-modeling-lifecycle` | Decision rule, P0–P9 phases, V1–V5 evidence, identity ownership and handoffs |
| `cratis-screenplay-discovery` | Personas, domain discovery, event timeline and rules register |
| `cratis-screenplay-slice-design` | Commands, refusals, views, field lineage and slice boundaries |
| `cratis-screenplay-streams-and-consistency` | Business identities, atomic invariants, concurrency, evolution and ownership |
| `cratis-screenplay-automations-and-translations` | Follow-up work, external data to our facts, recovery and deduplication |
| `cratis-screenplay-scenario-coverage` | Coverage matrix, rejection, denial, lifecycle and external-failure scenarios |
| `cratis-screenplay-model-review` | Independent critique, evidence checklist and business-question explanation |
| `cratis-screenplay-legacy-extraction` | Evidence-led extraction, expert verification and loss report |
| `cratis-screenplay-toolchain` | Compiler and CLI versions, verdict commands, diagnostics and executable subsets |
| `cratis-screenplay-render-and-gap-fill` | Admission, publication, target verification and contract-led hand-written delivery |
| `cratis-screenplay-delivery-planning` | Slice counts by type, dependency waves, issue tracking and estimates from the team's own history |

#### Language

| Skill | Covers |
| --- | --- |
| `cratis-screenplay-command-surface` | `command`, `event`, `validate`, `authorize`, `produces`, `concurrency`, `constraint`, `concept`, `$context` |
| `cratis-screenplay-projections` | The Projection Declaration Language and the `reducer` escape hatch |
| `cratis-screenplay-read-surface` | `readmodel`, `query`, `screen`, name resolution |
| `cratis-screenplay-ui-composition` | `layout`, templates, `form`, `contribute`, `ui profile`, `theme`, `$strings`, `file` |
| `cratis-screenplay-captures-and-reactions` | The Change Data Capture Language, `reaction`, `trigger` |
| `cratis-screenplay-specifications` | Given/when/then and the reference execution |
| `cratis-screenplay-model-authoring` | Typed MCP authoring, model navigation/refactoring, compiler diagnostics, and source-versus-executable readiness |

The canonical agents are **Screenplay Modeler** (`screenplay-modeler.md`) for
modeling and legacy extraction, **Screenplay Reviewer** (`screenplay-reviewer.md`)
for read-only critique or explanation, and **Screenplay Renderer**
(`screenplay-renderer.md`) for render probes and gap-fill handoffs. Hand-written
scope uses `cratis-application-slice-conformance`, available wherever
`cratis-application-slice-specifications` is selected through
`cratis/specifications/dotnet`, to check code against the model and its specs.

The profile also selects the corpus-owned Screenplay MCP declaration from
`mcp-servers.json`. The Cratis CLI hosts the server as `cratis screenplay mcp`
and registers a scoped entry for supported clients without replacing their other
servers. Inspect install/status results for adapter support or configuration
conflicts. The model is wherever the project keeps its `.play` files, else under `Source/` or
`src/`, else in a `Screenplay/` folder at the repository root; never under `.cratis/`.
Project-owned configuration can name a root explicitly.

**No-model startup requires Cratis CLI 3.46.1 or later.** With
`cratis screenplay mcp --project-root <project>`, that version resolves the root
without creating it; discovery, startup and proposals leave no model folder, and
the first `apply` write creates it. `cratis ai install` also creates no model
folder unless the project configured a root. Older CLIs create the resolved model root at
server startup (`Screenplay/` when no `.play` files, `Source/` or `src/` exist); upgrade before using the automatically selected MCP server in a
repository without a model. In Pi, when every selected path to Screenplay goes through an application profile and no root is configured, a missing, older or unavailable CLI leaves its MCP tools inactive with one informational UI notice (none in headless mode); an independent non-application path to Screenplay or a configured root still surfaces startup errors. See the toolchain skill's
[version reference](../../../skills/cratis-screenplay-toolchain/references/versions.md).

## How to Use Profiles

### 1. Select Your Profile

Choose the profile that matches your current work:

```json
{
  "schemaVersion": "1.0.0",
  "profiles": [
    "cratis/application/csharp",
    "cratis/engineering/csharp"
  ]
}
```

### 2. Profile Composition

Profiles can compose other profiles. When you select a parent profile, all child profiles are automatically included.

**How composition works:**
- Selecting `cratis/application` automatically includes all its child profiles (listed in the "Automatically Includes" column above)
- Selecting `cratis/full` includes all full-stack capabilities across C#, TypeScript, Elixir, and Kotlin
- Selecting `cratis/engineering` automatically includes all engineering convention profiles

**Examples:**

```json
{
  "schemaVersion": "1.0.0",
  "profiles": [
    "cratis/application"  // Automatically includes all child profiles
  ]
}
```

```json
{
  "schemaVersion": "1.0.0",
  "profiles": [
    "cratis/application/csharp"  // Includes: arc, arc/react, chronicle, components, fundamentals, language/csharp, screenplay, specifications/dotnet, specifications/typescript
  ]
}
```

```json
{
  "schemaVersion": "1.0.0",
  "profiles": [
    "cratis/full"  // Includes all full-stack capabilities
  ]
}
```

**Note:** The profile-catalog.json file defines the complete composition tree. When you select a parent profile, the system automatically resolves and includes all child profiles listed in the `composes` array.

### 3. Multi-Profile Work

You can work with multiple profiles simultaneously:

```json
{
  "profiles": [
    "cratis/application/csharp",      // Backend development
    "cratis/application/react",       // Frontend development
    "cratis/engineering/csharp"       // Engineering conventions
  ]
}
```

### 4. Language-Specific Profiles

Select language profiles when working with specific languages:

```json
{
  "profiles": [
    "cratis/language/csharp",
    "cratis/language/typescript"
  ]
}
```

### 5. Skills in agent harnesses

The canonical skills live in `.cratis/ai/skills/`. Supported harnesses expose
that tree through generated links or package integration; marketplace plugins
may point their `skills` field at `./skills`. Edit the canonical skill and its
profile-catalog entry in this repository, not a consuming repository's managed
copy or a generated harness adapter. Check reachability through the selected
profiles as well as plugin discovery, which can expose the whole skill tree.

## Profile-Specific Rules

Every rule file declares its profile in the frontmatter:

```markdown
---
profile: application
---
```

- **`profile: application`** - Rules for building applications on Cratis
- **`profile: framework`** - Rules for contributing to Cratis framework repos
- **No profile tag** - Universal rules that apply to both profiles

## Finding Profile Information

- **Full catalog:** [profile-catalog.json](../../../profile-catalog.json)
- **Application rules:** [general.md](../../../rules/general.md) (Application profile section)
- **Framework rules:** [framework.md](./framework.instructions.md)
- **Engineering conventions:** [csharp.md](./csharp.instructions.md), [typescript.md](./typescript.instructions.md), [go.md](./go.instructions.md), [go-engineering.md](./go-engineering.instructions.md)

## See Also

- [general.md](../../../rules/general.md) - Project instructions and profile overview
- [vertical-slices.md](./vertical-slices.instructions.md) - Application profile architecture
- [framework.md](./framework.instructions.md) - Framework profile architecture
- [profile-catalog.json](../../../profile-catalog.json) - Complete profile definitions
