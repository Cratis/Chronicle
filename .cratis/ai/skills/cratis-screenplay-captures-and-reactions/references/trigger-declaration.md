<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/trigger-declaration.md -->
# Declaring a trigger

This reference holds the `trigger` declaration, its name resolution, built-in triggers and registration.

## `trigger`

```screenplay
concept Repository : String

trigger BuildFinished
  description "CI reported a finished build on a watched repository"
  repository Repository
  outcome String
```

A trigger declares two things: **that the name exists**, and **what an occurrence
hands the reaction**. It deliberately declares nothing about what makes one occur —
that belongs to whatever produces it, and keeping it out is what lets the set stay
open. The type on a value is optional; a bare name is already a useful statement.

**Name resolution, in order:** events the document declares or imports → triggers
the document declares → triggers registered with the compiler, plus the built-in
host signals. A registration wins over a built-in of the same name. Only when all
three miss is the name reported unknown — a **warning** (`PLAY0248`), because the
name may be known outside the document. A malformed reaction body is an error. If an event
and a trigger share a name, the event wins for value validation and binding.

**Built-in triggers:** `Startup` and `Shutdown`. The clock is built in too, but
with its own syntax rather than a name.

⚠️ **An application trigger is not a UI event.** What a button click, a form
submit or entering a screen does is an *interaction*: an `on click` / `on submit`
/ `on enter` clause or a named `behavior`, never a declared `trigger` (see
`cratis-screenplay-ui-composition`). The two meet in two places: an interaction
can observe a declared trigger with `on <Trigger>` and fire one with
`raise <Trigger>`. A trigger named after a built-in interaction kind (`click`,
`submit`, `enter`, …) is an error (`PLAY0326`).

⚠️ Under a reaction trigger, `description`, `file`, `produces`, `invokes` and
`reads` are directives, so a value with one of those names is written with `@`
(`@file`, `@reads`).

**Registration** (for a name only an integration knows) happens through the
compiler's language registry, either name-only or with a shape. ⚠️ Stating **no**
values and stating **none** are different claims: a definition with no value list
says the registration does not describe the shape, so what a reaction takes is left
alone; one with an **empty** list says an occurrence carries nothing, so taking
something is reported. `Startup` and `Shutdown` are registered the second way.
