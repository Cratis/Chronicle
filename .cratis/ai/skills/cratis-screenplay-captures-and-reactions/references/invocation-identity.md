<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/invocation-identity.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Declare the actor behind a reaction invocation

## Contents

- Trusted system identity
- Checked example
- Diagnostics and target boundaries

## Trusted system identity

Screenplay supports one reaction-level line
`runs as system [role "<Role>" and role "<Role>"]`. Roles are optional, nonempty,
distinct quoted literals; personas and expressions are not accepted. Put it
under the reaction, not its trigger or invocation. The declaration selects
**ESM v10**. Without it, invocations remain caller-less; `given caller` never
supplies an actor to a reaction.

The reference evaluator authenticates a system principal with exactly the stated
roles. Claim conditions remain unknown, including under not; a satisfied role
alternative may allow, but final unknown denies. This is not the caller's audit
identity and does not invent Arc's system subject claims. The identity covers
invoked/returned commands across that reaction's triggers, not imperative
pipeline calls in bodies, direct productions, reads, refusal productions or
causation. Record the actual least-privilege role, not a prose-only actor or an
ungated command substituted to make the scenario pass.

## Checked example

This complete model compiled with warnings as errors and its reference scenario
passes on the standalone compiler:

```screenplay
policy Worker
  require role "Worker"
module Work
  feature Processing
    slice StateChange RecordWork
      command RecordWork
        workId String identifier
        authorize Worker
        produces event WorkRecorded
    slice Automation ProcessWork
      event WorkRequested
        workId String
      reaction Processor
        runs as system role "Worker"
        when WorkRequested
          workId
          invokes RecordWork
            workId = workId
      specification ProcessingRequestedWork
        when append WorkRequested
          workId = "work-1"
        then WorkRecorded
          for "work-1"
```

## Diagnostics and target boundaries

- PLAY0647: malformed, repeated or misplaced identity, including duplicate/empty roles.
- PLAY0648: gated invocation without declared identity and without an authorization
  refusal branch. PLAY0557 instead warns on that refusal branch; it cannot turn
  an always-denied caller-less invocation into a trusted path.
- PLAY0649: identity with neither invocations nor an implementation body.
- C# binding PLAY0650/0651 warns about unused roles or definite authorization
  denial; opaque/unknown cases are not guessed. `--check privilege` (PLAY0652)
  reviews whether event producers require every elevated role, not runtime safety.

An older CLI-bundled compiler does not parse `runs as`:
the example's `cratis screenplay validate --executable --warnings-as-errors`
returns PLAY0137, exit 5. The CLI/Stage pair
(`cratis-screenplay-toolchain/references/renderable-subset.md`) renders role-only/authenticated-only reaction invocation gates with `[ExecuteCommandsAsSystem]`;
claim-reading or opaque gates still refuse STAGE-ESM-034 (including claim reads under `not`/`or`).
Source: Stage `Source/Rendering.Cratis/Semantics/SemanticCratisAdmission.ReactionIdentity.cs`;
`Documentation/reference/reaction-rendering.md`. A renderer that stops at v7 audits
through v7 only (toolchain `references/renderable-subset.md`).
Clock/application-trigger identities have no Stage Arc realization either.
Use authorized gap-fill with the model as contract; returned-command Arc identity
uses `[ExecuteCommandsAsSystem]`, never permission inferred from a model header.
Redelivery/no-event assertions execute as v17 and refusal branches as v19;
identity admission alone does not establish a target's support for either.
Read [refusals and redelivery](refusals-and-redelivery.md) when combining them.

Authority: Screenplay `reactions.md`, `ReactionIdentityParser.cs`,
`ReactionRefusalValidator.cs`, `SemanticModelBinder.cs`; Stage `SemanticCratisAdmission.cs`.
