<!-- cratis-ai-managed: skills/cratis-screenplay-availability/references/review-checks.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Review checks for availability and context values

Run these on any model that declares `audience`, `availability`, `available` or `provide`. Report a finding
with the construct address and the change to ask for; do not edit the model in a review.

## Gates

1. **A gate no persona can satisfy.** For each `available for` (through `with`, `and` and `or`), find a persona whose
   authoring `audience` lines satisfy it. If none can, the part is unreachable by every persona: the audience is
   missing from personas, mistyped, or the gate is wrong. Personas are authoring only, so this is a model-quality
   check, not a runtime one.
2. **Unreferenced audiences.** An `audience` that no `available for`, `availability` `for` or persona names is dead.
   Ask whether it is a leftover or a missing gate.
3. **Undeclared names.** PLAY1135 and PLAY1136 mean nothing can grant the audience or the availability, so the gate
   is permanently closed. Treat as a defect, not noise.
4. **Gate on the wrong level.** A gate repeated on every slice of a feature belongs on the feature. A gate that
   hides something a sibling still `invokes` triggers the cross-slice warning (PLAY1139); check it is intended.
5. **Availability used as authorization, or the reverse.** A role, claim or ownership test in `available when`, or a
   package or rollout in a `policy`, is in the wrong mechanism.
6. **Specifications.** None may depend on a gate; specifications always run with everything available. A
   specification written to prove a closed gate is invalid.
7. **Audience name confusion.** No gate reads `$context.identity.claims.aud` as if it were an audience, and no event
   carries Chronicle's routing audience as a field.
8. **Entitlements.** A package already enforced by Box Office modules is not also modeled as an audience unless
   the model deliberately reads a host value.

## Provided values

1. **A `provide` whose key cannot be resolved for a command.** `$context.arguments.<key>` is the query's input and
   has no meaning for a command; a command needs `$context.command.<key>`. A feature-level `provide ... from query`
   that serves both a command and a query cannot name the key for both, so ask for a slice-level override.
   A required value (no `optional`) that a command cannot resolve refuses the command.
2. **A rule reading identity-derived values.** `RuleContext` has no `Identity`. A `provide` derived from
   `$context.identity` or `$context.causedBy` that a rule reads smuggles roles or claims into validation. Ask for it
   to move to a `policy`, or for the reason to be stated.
3. **Reserved or overriding names.** A name equal to a built-in is PLAY1124; an override with another type is PLAY1125.
4. **Source quality.** A `query` source must be keyed, snapshot and not observable; opaque `host`, code and `file`
   sources mean the reference evaluator cannot decide a gate that reads them.
5. **Required versus optional.** Optional means "can be absent and the gate denies"; required means "absence refuses
   the request". Check each value matches the intent for its readers.
6. **Executable claims.** Do not claim a `provide` runs end to end while the toolchain reports PLAY0268 for it.
7. **Unprovided reads.** An `available when` reading a value nothing provides at that place (PLAY1138) stays closed.

## Verdict wording

Report the checks run, each finding with severity, and what was not checked (runtime audience grants, opaque conditions).
