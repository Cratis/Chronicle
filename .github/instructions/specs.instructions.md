---
applyTo: "**/for_*/**/*.*, **/when_*/**/*.*"
---

# How to Write Specs

We call automated tests **specs** (specifications), not tests. This is deliberate — specs are executable documentation that describe what the system *does*, written in the language of the domain. A new developer should be able to read the spec folder structure like a table of contents and understand the system's behavior without opening a single source file.

This philosophy comes from Specification by Example and BDD (Behavior Driven Development). The goal is human-readable, navigable specifications that double as a living contract.

## Core Philosophy

- **Specify behaviors, not implementations.** A spec should verify what a method *promises from its signature* — its contract with callers. If the implementation changes but the contract holds, specs should still pass. When they don't, the spec was testing the wrong thing.
- **One behavior, one spec.** Every public method that performs an action gets its own `when_` folder or file. Never bundle multiple behaviors into one spec — it obscures what broke and why.
- **Specs are documentation first.** The folder tree, class names, and `should_` assertions form a specification anyone can read. Optimize for readability over DRY. A little repetition in setup is fine if it makes the spec self-contained and clear.
- **Do not test logging** — it is too fragile and provides no value. Don't test simple delegation or trivial getters either. The cost of maintaining these specs exceeds the value they provide.

## Folder & File Structure

Specs mirror the source structure and read like a sentence when you trace the path: `for_Changeset / when_adding_changes / and_there_are_differences`. This is not accidental — the folder hierarchy *is* the specification. Every level adds context:

```
for_<TypeUnderTest>/
├── given/
│   ├── all_dependencies           ← common DI/mock setup
│   └── a_<descriptive_name>       ← reusable context
├── when_<behavior>/               ← folder for behaviors with multiple outcomes
│   ├── given/                     ← behavior-specific context (optional)
│   │   └── a_<specific_setup>
│   ├── and_<condition>            ← individual spec file
│   ├── with_<data_state>
│   └── without_<requirement>
└── when_<simple_behavior>         ← single file for single-outcome behaviors
```

**Naming conventions — read them as English sentences:**
| Element | Pattern | Reads as... |
|---|---|---|
| Unit folder | `for_<ClassName>` | "For the Changeset..." |
| Behavior folder | `when_<action>` | "...when adding changes..." |
| Spec file | Descriptive preposition | "...and there are differences" |
| Assertion | `should <expected result>` | "...it should return true" |

**Allowed prepositions for spec file/class names:**
- `and_*` — additional conditions or compound scenarios
- `with_*` / `without_*` — specific data or state present/absent
- `having_*` — possession or state-based conditions
- `given_*` — precondition scenarios

## What to Specify

The goal is to cover *decisions and transformations* — code where bugs hide. Simple plumbing that the compiler already validates is noise.

**Write specs for:**
- Public methods that perform actions (behaviors)
- Methods with branching logic or business rules
- Methods that coordinate between dependencies

**Do NOT write specs for:**
- Simple auto-properties (`public string Name { get; set; }`)
- Properties returning constructor parameters (`public Key Key => key;`)
- Simple delegation (`public IEnumerable<Property> Properties => mapper.Properties;`)
- Trivial null checks or basic validation without complex logic
- Getters returning injected dependencies

**Avoid file names starting with:** `when_getting_*`, `when_returning_*` — if a spec name starts with "getting" or "returning", it's probably testing a simple getter, which is not worth specifying.

## Multiple Outcomes

Each distinct outcome deserves its own spec file. This keeps specs small, focused, and independently verifiable. When a spec fails, you immediately know *which* outcome broke — no debugging through a multi-assertion file.

- When a behavior has multiple outcomes, create a `when_<behavior>/` folder with separate files for each outcome.
- For simple behaviors with a single outcome, use a single file: `when_<behavior>`.
- Never write a single file that tests an entire class.

## Reusable Context

Contexts capture the "given" part of a specification — the world as it exists before the action under test. They prevent duplicating setup across specs while keeping each spec readable.

- Place in `given/` folder within the unit folder.
- Name with `a_` or `an_` prefix (e.g. `an_observer`, `a_command_pipeline`). This reads naturally: "given an observer, when handling..."
- More specific contexts can use descriptive names (e.g. `two_queries`, `existing_query`).
- Context properties must be accessible to the specs that use them — see language-specific instructions for the exact access modifiers and naming conventions.
- Use the setup phase for context initialization — **never** put the action under test in a reusable context. The action under test belongs only in the concrete spec.
- Contexts can build on each other in layers: `all_dependencies → a_reactor_handler → when_handling`.
- Consider creating `all_dependencies` as a root context that mocks all common dependencies. This avoids duplicating mock creation across unrelated specs.

## Formatting

- Keep assertions concise — prefer single-line assertions where the logic is readable.
- Don't add blank lines between related assertions for the same behavior.
