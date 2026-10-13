<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/invocation-identity.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Declare the actor behind a reaction invocation

## Contents

- Trusted system identity
- Checked example
- Diagnostics and target boundaries

## Trusted system identity

Screenplay 4.125.0 supports one reaction-level line
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
passed on standalone Screenplay 4.125.0:

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

Historical CLI 3.41.0 bundled Screenplay 4.114.0, which did not parse `runs as`:
the example's `cratis screenplay validate --executable --warnings-as-errors`
returned PLAY0137, exit 5. This is not a current parser claim. The pinned CLI/Stage pair
(`cratis-screenplay-toolchain/references/versions.md`) renders role-only/authenticated-only reaction invocation gates with `[ExecuteCommandsAsSystem]`;
claim-reading or opaque gates still refuse STAGE-ESM-034 (including claim reads under `not`/`or`).
Source: Stage `v4.89.1:Source/Rendering.Cratis/Semantics/SemanticCratisAdmission.ReactionIdentity.cs:11-28`;
`Documentation/reference/reaction-rendering.md:57-59`. Previous CLI 3.46.1/Stage 4.52.0
audited through v7 only (`references/versions.md` in toolchain).
Clock/application-trigger identities have no Stage Arc realization either.
Use authorized gap-fill with the model as contract; returned-command Arc identity
uses `[ExecuteCommandsAsSystem]`, never permission inferred from a model header.
Refusal/redelivery syntax remains unadmitted even when identity itself runs.

Authority: Screenplay v4.125.0 `reactions.md`, `ReactionIdentityParser.cs`,
`ReactionRefusalValidator.cs`, `SemanticModelBinder.cs`; CLI v3.41.0
`RenderedSemanticVersions.cs`; Stage v4.51.3 `SemanticCratisAdmission.cs`.
