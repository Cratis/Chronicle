<!-- cratis-ai-managed: skills/cratis-application-slice-specifications/references/reading-play-specifications.md -->
# Reading a .play specification

This reference holds the mapping from `.play` specification assignments to scenario setup and assertions.

### Reading a `.play` specification

Read the actual assignments, not a prose retelling; syntax and supported forms belong to
`cratis-screenplay-specifications`.
- `given <Event>` becomes prior facts seeded with `Given.ForEventSource(...).Events(...)`.
  `given readmodel <Type>` becomes a pinned complete `.ReadModel(...)` instance, including its
  key: the contract deliberately makes event history irrelevant here. Protected decisions still
  need event-backed guards (Step 5); report a pin that cannot witness the required enforcement.
- `when <Command>` assignments fill that command; `given caller` supplies the scenario's caller,
  not an invented default role. Use the matching scenario for append, query or reaction actions.
- `then <Event>` assignments become the appended-event predicate;
  `then readmodel` assignments become instance assertions. Assert every named value and honor
  `exactly`, ordering and absence where stated. Do not assert an unmentioned generated identifier
  or timestamp; still use required source identities to select the result. A broader property
  coverage gap goes back to the contract owner rather than adding a new business assertion.
- `description` and slice `documentation` provide context for a name or XML summary, not extra
  setup, assertions or inferred rules. Explicit realization requirements still bind delivery;
  reconcile them with executable parts through `cratis-application-slice-conformance`.
- A `parameter`/`case` table yields independent examples by substituting each `case.<parameter>`
  value. The reference runner identifies each row as `<Specification>_<Case>`; retain that name
  in the specification map, while C# uses the repository's `when_.../and_<case>` convention.
  Name parity is a mapping requirement, not a requirement to rename C# classes.
- `then error` requires unsuccessful execution plus the named validation/constraint reason where
  stated (Step 6). A specified message must be checked, using equality when exact wording is
  contracted. `then denied` instead requires unsuccessful execution and `ShouldNotBeAuthorized()`;
  it is not a validation error. Keep every other rule satisfied so the named refusal is witnessed.
