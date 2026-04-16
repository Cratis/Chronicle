# Claude Code Instructions

This project uses GitHub Copilot instructions, skills, agents, prompts, and hooks stored in `.github/`. This file maps them to Claude Code behavior. **Never duplicate content from those files — read them directly when relevant.**

---

## Project foundation

Read `.github/copilot-instructions.md` at the start of every session. It contains:
- Project philosophy (lovable APIs, vertical slices, full-stack type safety, consistency)
- General rules (American English, compile-clean, no warnings, package/config file protection)
- Development workflow (build after each file, fix errors immediately, run specs before finishing)
- File header required on all C# files

---

## Instruction files — read when working in matching contexts

These live in `.github/instructions/`. Read the relevant file before starting work in its domain:

| File | Read when working with |
|---|---|
| `csharp.instructions.md` | Any `.cs` file |
| `typescript.instructions.md` | Any `.ts` or `.tsx` file |
| `vertical-slices.instructions.md` | Any `Features/` folder — architecture rules, slice types, folder layout |
| `specs.instructions.md` | Any `for_*/` or `when_*/` folder — spec philosophy and naming |
| `specs.csharp.instructions.md` | C# spec files |
| `specs.typescript.instructions.md` | TypeScript spec files |
| `code-quality.instructions.md` | Code review, refactoring, or quality judgments |
| `code-quality.csharp.instructions.md` | C# code quality details |
| `code-quality.typescript.instructions.md` | TypeScript code quality details |
| `components.instructions.md` | React component files |
| `dialogs.instructions.md` | Dialog components (always use Cratis wrappers, never primereact directly) |
| `reactors.instructions.md` | `IReactor` implementations |
| `concepts.instructions.md` | `ConceptAs<T>` domain value types |
| `efcore.instructions.md` | Entity Framework Core |
| `efcore.specs.instructions.md` | EF Core spec files |
| `orleans.instructions.md` | Orleans grain code |
| `documentation.instructions.md` | Writing documentation |
| `pull-requests.instructions.md` | Creating or reviewing PRs |
| `git-commits.instructions.md` | Making commits — logical grouping, message format, staging discipline |
| `terminal-commands.instructions.md` | Running terminal commands |
| `web-fetching.instructions.md` | Fetching URLs or external content |

---

## Skills — read before performing these task types

Skills live in `.github/skills/<skill-name>/SKILL.md`. Read the SKILL.md (and its `references/` files as needed) before starting the corresponding work:

| Skill folder | When to use |
|---|---|
| `cratis-vertical-slice/` | Understanding vertical slice structure, folder layout, slice types |
| `new-vertical-slice/` | Building a new slice end-to-end (backend → build → frontend) |
| `scaffold-feature/` | Creating a new feature folder from scratch |
| `cratis-command/` | Adding a command to an existing slice |
| `cratis-readmodel/` | Adding a read model and projection |
| `add-projection/` | Adding a projection to an existing read model |
| `add-concept/` | Adding a `ConceptAs<T>` domain value type |
| `add-reactor/` | Adding an `IReactor` |
| `add-ef-migration/` | Adding an EF Core migration |
| `add-business-rule/` | Adding a business rule / constraint |
| `cratis-csharp-standards/` | C# code standards reference |
| `cratis-react-page/` | React page patterns (MVVM, data tables, data pages) |
| `cratis-specs-csharp/` | C# spec patterns |
| `cratis-specs-typescript/` | TypeScript spec patterns |
| `review-code/` | Code review — read `references/CHECKLISTS.md` |
| `review-security/` | Security review |
| `review-performance/` | Performance review |
| `write-documentation/` | Writing documentation for features or APIs |
| `stepper-command-dialog/` | Stepper-style command dialogs |
| `toolbar/` | Toolbar components |
| `auth-and-identity/` | Authentication and authorization patterns |

---

## Agents — specialized role personas

Agent definitions live in `.github/agents/`. Reference these when the user asks you to act in a specific role or when coordinating complex multi-concern work:

| Agent | Role |
|---|---|
| `orchestrator.md` | Top-level coordinator — assembles teams of agents for complex goals |
| `coordinator.md` | Implementation coordinator — backend + frontend + reviews |
| `planner.md` | Vertical slice planner — end-to-end slice from C# to React |
| `backend-developer.md` | C# backend implementation only |
| `frontend-developer.md` | React/TypeScript frontend only |
| `spec-writer.md` | BDD integration specs (C#) and unit specs (TypeScript) |
| `code-reviewer.md` | Architecture conformance, C# and TypeScript standards |
| `security-reviewer.md` | Security vulnerabilities, auth/authz, data exposure |
| `performance-reviewer.md` | Chronicle projections, MongoDB queries, React overhead |

---

## Prompts — reusable workflow templates

Prompt files live in `.github/prompts/`. When the user asks for one of these operations, read the corresponding prompt file for the full workflow:

| Prompt file | Operation |
|---|---|
| `new-vertical-slice.prompt.md` | Build a complete new vertical slice |
| `scaffold-feature.prompt.md` | Scaffold a new feature folder |
| `add-concept.prompt.md` | Add a `ConceptAs<T>` type |
| `add-business-rule.prompt.md` | Add a business rule / constraint |
| `add-projection.prompt.md` | Add a projection |
| `add-reactor.prompt.md` | Add a reactor |
| `add-ef-migration.prompt.md` | Add an EF Core migration |
| `write-specs.prompt.md` | Write specs for existing code |
| `write-documentation.prompt.md` | Write documentation |
| `review-pr.prompt.md` | Review a pull request |

---

## Hooks — behavioral rules

### Pre-commit (`.github/hooks/pre-commit.md`)

Before running any `git commit` command:
1. Identify affected projects from staged files (`git diff --name-only --cached`)
2. Run `dotnet test <specs-project>` for each affected `.csproj`
3. Run `yarn test` for each affected TypeScript package
4. If any spec fails — do NOT commit; fix failures first
5. Only proceed with the commit when all specs pass

### Post-task quality gate

At the end of every task:
1. `dotnet clean && dotnet build -c Release` — must be zero warnings, zero errors
2. Run specs for every affected project — must all pass
3. `yarn compile` for TypeScript — must be zero errors
4. After pushing to a PR — monitor CI checks and fix any failures

---

## Key conventions (quick reference)

- **Vertical slices**: all backend artifacts for one behavior in a single `.cs` file; backend before frontend; `dotnet build` generates TypeScript proxies
- **No separate handler classes**: `Handle()` lives directly on the `[Command]` record
- **`[EventType]` takes no arguments** — the type name is the identifier
- **American English only** in all identifiers, comments, and docs
- **Never modify** `package.json`, `global.json`, `NuGet.config`, or anything in `node_modules/`
- **File header required** on all C# files (see `copilot-instructions.md`)
- **Specs not tests** — folder naming: `for_<Type>/when_<behavior>/and_<outcome>.cs`
