---
applyTo: "**/.cratis/**"
paths:
  - "**/.cratis/**"
---
<!-- cratis-ai-managed: rules/profiles.md -->

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
- [vertical-slices.md](./vertical-slices.md) - slice anatomy and structure
- [react.md](./react.md) - React + Arc + Cratis Components
- [components.md](./components.md) - component structure and styling
- [dialogs.md](./dialogs.md) - dialog patterns
- [specs.scenarios.csharp.md](./specs.scenarios.csharp.md) - in-process scenario family

### Framework Profile

**Use when:** You are contributing to a Cratis framework repository itself (Arc, Chronicle, Fundamentals, Components, Specifications).

**What it includes:**
- Library development (not applications)
- Source generators, the Chronicle kernel (Orleans grains + storage), client SDKs, React component library
- No vertical slices, no model-bound `[Command]`/`[ReadModel]` artifacts
- No projections/read-models, no MVVM app components

**Key rules:**
- [framework.md](./framework.md) - repo structure and library/API design
- [orleans.md](./orleans.md) - Orleans grain conventions
- [specs.csharp.md](./specs.csharp.md) - universal `Specification` base + NSubstitute

## Profile Catalog

The complete list of available profiles is defined in [profile-catalog.json](../profile-catalog.json). This catalog includes:

### Application Profiles

| Profile ID | Description | Automatically Includes |
|---|---|---|
| `cratis/application` | Full application stack (C# + React + TypeScript) | `cratis/application/csharp`, `cratis/application/elixir`, `cratis/application/kotlin`, `cratis/application/typescript`, `cratis/arc/core`, `cratis/arc/react`, `cratis/chronicle/core`, `cratis/components`, `cratis/fundamentals`, `cratis/specifications/dotnet`, `cratis/specifications/typescript` |
| `cratis/application/csharp` | C# backend with Arc + Chronicle | `cratis/arc`, `cratis/arc/react`, `cratis/chronicle`, `cratis/components`, `cratis/fundamentals`, `cratis/language/csharp`, `cratis/specifications/dotnet`, `cratis/specifications/typescript` |
| `cratis/application/react` | React frontend with Cratis Components | `cratis/arc/core`, `cratis/arc/react`, `cratis/components`, `cratis/fundamentals`, `cratis/specifications/dotnet`, `cratis/specifications/typescript` |
| `cratis/application/typescript` | TypeScript client for Chronicle | `cratis/chronicle/client-typescript`, `cratis/language/typescript`, `cratis/specifications/typescript` |
| `cratis/application/arc-chronicle` | Arc + Chronicle integration | `cratis/arc/core`, `cratis/chronicle/core`, `cratis/fundamentals`, `cratis/specifications/dotnet` |
| `cratis/application/arc-only` | Arc without Chronicle | `cratis/arc/core`, `cratis/fundamentals`, `cratis/specifications/dotnet` |
| `cratis/application/chronicle-dotnet` | Chronicle .NET client | `cratis/chronicle/client-dotnet`, `cratis/chronicle/core`, `cratis/fundamentals`, `cratis/specifications/dotnet` |
| `cratis/application/elixir` | Elixir Chronicle client | `cratis/chronicle/client-elixir`, `cratis/language/elixir` |
| `cratis/application/kotlin` | Kotlin Arc + Chronicle application | `cratis/arc/client-kotlin`, `cratis/chronicle/client-kotlin`, `cratis/language/kotlin` |
| `cratis/application/java` | Java Arc + Chronicle application | `cratis/arc/client-kotlin`, `cratis/chronicle/client-java`, `cratis/language/java` |

### Framework Profiles

| Profile ID | Description | Automatically Includes |
|---|---|---|
| `cratis/arc` | Arc CQRS framework | `cratis/arc/csharp`, `cratis/arc/java`, `cratis/arc/kotlin` |
| `cratis/chronicle` | Chronicle event sourcing engine | `cratis/chronicle/compliance`, `cratis/chronicle/csharp`, `cratis/chronicle/elixir`, `cratis/chronicle/java`, `cratis/chronicle/kotlin`, `cratis/chronicle/multi-tenancy`, `cratis/chronicle/typescript`, `cratis/chronicle/web-workbench` |
| `cratis/components` | React component library | (no child profiles) |
| `cratis/fundamentals` | Core primitives (`ConceptAs<T>`, `EventSourceId<T>`) | (no child profiles) |
| `cratis/specifications` | Specification framework | (no child profiles) |

### Engineering Profiles

| Profile ID | Description | Automatically Includes |
|---|---|---|
| `cratis/engineering` | Engineering conventions and workflows | `cratis/engineering/core`, `cratis/engineering/csharp`, `cratis/engineering/elixir`, `cratis/engineering/kotlin`, `cratis/engineering/react`, `cratis/engineering/typescript` |
| `cratis/engineering/csharp` | C# engineering conventions | `cratis/engineering/core` |
| `cratis/engineering/typescript` | TypeScript engineering conventions | `cratis/engineering/core` |
| `cratis/engineering/react` | React engineering conventions | `cratis/engineering/core` |

### Language Profiles

| Profile ID | Description |
|---|---|
| `cratis/language/csharp` | C# language conventions |
| `cratis/language/typescript` | TypeScript language conventions |
| `cratis/language/elixir` | Elixir language conventions |
| `cratis/language/kotlin` | Kotlin language conventions |
| `cratis/language/java` | Java language conventions |

### Arc and Chronicle on the JVM (Kotlin and Java)

Arc.Kotlin (`io.cratis:arc`) and its optional Chronicle integration
(`io.cratis:arc-chronicle-spring-boot-starter`) bring the same command/query
model-bound shape to the JVM that Arc .NET brings to C#, for both Kotlin and
Java application code. `cratis/arc/client-kotlin` carries the skills for both
languages — Java application code still needs Kotlin and KSP on the build,
since Arc generates Kotlin adapters for Java declarations.

| Skill | Covers |
| --- | --- |
| `cratis-arc-command-kotlin` | `@Command`, `handle()`/`provide()`, Chronicle event responses, command authorization, TypeScript proxy generation |
| `cratis-arc-query-kotlin` | `@ReadModel` queries, GET vs RFC QUERY, observable queries (`Flow`, `Flow.Publisher`, RxJava 3) over SSE/WebSocket |
| `cratis-arc-validation-kotlin` | `FluentModelValidator` shared rules, `CommandValidator`/`QueryValidator`/`ConceptValidator`/`ModelValidator`, Jakarta constraints |

Standalone Chronicle client usage (no Arc) for Kotlin and Java is
`cratis-chronicle-client-kotlin`, reused by `cratis/chronicle/client-java`.
JVM language conventions are `kotlin.md` and `java.md`.

### Specialized Profiles

| Profile ID | Description |
|---|---|
| `cratis/documentation` | Reader-centered product docs, technical examples, release notes, and voice review |
| `cratis/content` | Release notes, social feed posts, voice review, content length, and developer marketing and distribution |
| `cratis/review` | Code review, performance, security |
| `cratis/studio` | Studio MCP safety guidance |
| `cratis/cli` | CLI operations |
| `cratis/lens` | Lens browser extension |
| `cratis/screenplay` | Event modeling and information-system design with Screenplay — the method and the whole `.play` language |
| `cratis/stage` | Stage rendering and sandbox |
| `cratis/modeling/screenplay-stage` | Screenplay + Stage together |

### Event modeling with Screenplay

`cratis/screenplay` carries the **method** and the **language**, split one skill
per surface so only the relevant one loads:

| Skill | Covers |
| --- | --- |
| `cratis-screenplay-event-modeling` | Domain discovery, the nine-step workflow, the four slice types, model validation |
| `cratis-screenplay-command-surface` | `command`, `event`, `validate`, `authorize`, `produces`, `concurrency`, `constraint`, `concept`, `$context` |
| `cratis-screenplay-projections` | The Projection Declaration Language and the `reducer` escape hatch |
| `cratis-screenplay-read-surface` | `readmodel`, `query`, `screen`, name resolution |
| `cratis-screenplay-ui-composition` | `layout`, templates, `form`, `contribute`, `ui profile`, `theme`, `$strings`, `file` |
| `cratis-screenplay-captures-and-reactions` | The Change Data Capture Language, `reaction`, `trigger` |
| `cratis-screenplay-specifications` | Given/when/then and the reference execution |
| `cratis-screenplay-model-authoring` | Typed MCP authoring, model navigation/refactoring, compiler diagnostics, and source-versus-executable readiness |

The profile also selects the corpus-owned Screenplay MCP declaration from
`mcp-servers.json`. The Cratis CLI hosts the server as `cratis screenplay mcp`
and registers a scoped entry for supported clients without replacing their other
servers. Inspect install/status results for adapter support or configuration
conflicts. The conventional model root is `.cratis/screenplay/`; project-owned
configuration can choose another root.

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
    "cratis/application/csharp"  // Includes: arc, arc/react, chronicle, components, fundamentals, language/csharp, specifications/dotnet, specifications/typescript
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

- **Full catalog:** [profile-catalog.json](../profile-catalog.json)
- **Application rules:** [general.md](./general.md) (Application profile section)
- **Framework rules:** [framework.md](./framework.md)
- **Engineering conventions:** [csharp.md](./csharp.md), [typescript.md](./typescript.md)

## See Also

- [general.md](./general.md) - Project instructions and profile overview
- [vertical-slices.md](./vertical-slices.md) - Application profile architecture
- [framework.md](./framework.md) - Framework profile architecture
- [profile-catalog.json](../profile-catalog.json) - Complete profile definitions
