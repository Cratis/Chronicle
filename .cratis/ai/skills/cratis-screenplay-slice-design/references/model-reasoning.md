<!-- cratis-ai-managed: skills/cratis-screenplay-slice-design/references/model-reasoning.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Keep reasoning on the model

Use `description` for a short summary and `documentation` for assumptions,
boundaries, trade-offs and rejected alternatives. Put reasoning on the declaration
it explains, not in comments or disconnected session notes. Modules, features,
slices, commands, events, read models and reactions accept one nonempty block,
directly in their body (not beneath a reaction trigger). Queries, personas and other
declarations do not accept `documentation`: keep their intent in `description` (a
query's ordering, "oldest first", for example). A short status marker such as **NOT
enforced in the model today** or **ASSUMED** stays in `description`, where a reader
sees it first; the reasoning behind it goes in `documentation`:

````screenplay
module Billing
  documentation
    ```markdown
    ## Boundary
    Billing records agreed amounts; payment delivery belongs elsewhere.
    ```
  feature Amounts
    documentation
      ```markdown
      Keep the agreed amount, not a running total.
      ```
    slice StateChange RecordAmount
      documentation
        ```markdown
        A recorded amount is a decision, not a recalculated view.
        ```
      command RecordAmount
        documentation
          ```markdown
          The caller supplies the agreed amount.
          ```
        recordId String identifier
        amount Int
        produces event AmountRecorded
          amount Int = amount
      specification RecordingAnAmount
        description "Witnesses that the agreed amount is retained"
        when RecordAmount amount = 10
          recordId = "amount-1"
        then AmountRecorded amount = 10
    slice StateView AmountHistory
      readmodel AmountView
        documentation
          ```markdown
          This view is display state, not an atomic decision input.
          ```
        id String
        amount Int
      projection AmountHistory => AmountView
        from AmountRecorded
          id = $eventSourceId
          amount = amount
      query AmountById => AmountView optional
        by id String
    slice Automation ObserveAmount
      reaction ObserveAmount
        documentation
          ```markdown
          Observation does not certify an exactly-once external effect.
          ```
        when AmountRecorded
````

A specification accepts a `description` naming the behavior it witnesses, not
`documentation`; longer reasoning belongs to its slice. Projections and screens
do not accept documentation. `documentation String` remains a typed property
where properties are allowed; bare `documentation` starts metadata.

Both metadata fields are report-only (PLAY0270): no executable bytes,
`modelRevision` or ESM version change, no rule enforcement or rendered code.
Canonical printing retains documentation on its owner; source identity changes. MCP `declaration-details` summary exposes
`description` and `documentation` wherever supported. Folder merge keeps the
first module/feature block: identical copies are accepted, conflicting copies
warn PLAY0559. Malformed, empty or repeated blocks report PLAY0558; events retain
PLAY0477. These diagnostics have no automatic repair; choose the owning text.

Authority: Screenplay v4.127.0 `slices.md` and `specifications.md`. The complete
model above validates and its specification passes on standalone 4.127.0.
