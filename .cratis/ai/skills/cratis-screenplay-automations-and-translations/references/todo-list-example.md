<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/todo-list-example.md -->
# Example: a todo-list automation

## Contents

- Design-only additions

A complete model: a result opens a work item, a reaction invokes a command, the command's fact
closes the item. It binds as ESM v6 (`executableReady: true`), standalone and through `cratis screenplay mcp`.
Stage renders supported event reactions, but this model's clock/cascade specs remain
refused (`-024`/`-011`); keep them intact. A compiler that stops before v6 rejects the
Automation slice at binding with PLAY0285 or PLAY0268. Versions: `cratis-screenplay-toolchain` `SKILL.md`.

The first spec lists the whole cascade (`then` names every new fact: `CoursePassed`, and
`CertificateSent` through the reaction), and `given clock` supplies the instant that
`$context.occurred` needs. Both tools accept that cascade when they admit v6; an older compiler reports a false PLAY0285 on it.
Current admitted scenarios can be run with
standalone `screenplay test` or MCP `run-specifications`.

What the model proves and what it does not:

- Proven by the model: one `CertificateSent` per enrolment (`unique event`), the queue opening
  and closing, a gated `RecordCompletion` that refuses non-coordinators.
- Not proven: that the certificate email reaches the person once. See
  `references/effects-and-idempotency.md`.
- The actor behind `SendCertificate` is a recorded security decision in the `Certification`
  module `description`: this example declares no reaction identity, so its
  invocations remain caller-less. A gated trusted path can instead declare
  `runs as system role` (ESM v10), with separate CLI/Stage refusal.
- Report reference outcomes only after executing the selected scenarios.

```screenplay expect PLAY0469,PLAY0516
// Todo-list automation. Shape: CoursePassed opens an item -> CertificateSender invokes
// SendCertificate -> CertificateSent closes the item. The once-only FACT is
// `unique event CertificateSent`; the external send is NOT proven once-only by this model.

domain Acme.Academy

concept EnrolmentId : Uuid

policy IsAuthenticated
  require authenticated
policy IsCoordinator
  require role "Coordinator"
persona Coordinator
  description "Records course results. Cannot: send certificates by hand"
  policy IsAuthenticated
  policy IsCoordinator

module Courses
  authorize IsAuthenticated
  feature Results
    authorize IsCoordinator
    slice StateChange RecordCompletion
      command RecordCompletion
        enrolmentId EnrolmentId identifier
        passed      Bool
        produces when passed == true
          CoursePassed
            for enrolmentId
            enrolmentId = enrolmentId          // copied on purpose: the reaction selects it as a value
            completedAt = $context.occurred    // carried on the fact: a projection cannot read $eventContext.occurred
        produces when passed == false
          CourseFailed
            for enrolmentId
      event CoursePassed
        enrolmentId EnrolmentId
        completedAt DateTime
      event CourseFailed
      constraint OneResultPerEnrolment
        unique event CoursePassed
        unique event CourseFailed
      specification RecordingAPass
        given caller
          authenticated
          role "Coordinator"
        given clock "2026-10-05T08:00:00Z"
        when RecordCompletion
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          passed      = true
        then CoursePassed
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          completedAt = "2026-10-05T08:00:00Z"
        then CertificateSent
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          sentAt = "2026-10-05T08:00:00Z"
      specification RefusingANonCoordinator
        given caller
          authenticated
          role "Learner"
        when RecordCompletion
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          passed      = true
        then denied
      specification RecordingAFail
        given caller
          authenticated
          role "Coordinator"
        given clock "2026-10-05T08:00:00Z"
        when RecordCompletion
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          passed      = false
        then CourseFailed
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"

module Certification
  description "Trusted background boundary. No persona or screen issues SendCertificate; only the CertificateSender reaction invokes it, and reaction invocations carry no caller. Leaving the command ungated is a SECURITY DECISION, accepted only because: (1) it appends a fact keyed to one enrolment, (2) a repeat is rejected by IssueOnce, (3) the realization must not expose it to external callers (requirement to verify in the target). If (3) cannot hold, gate it with a service-actor policy and declare runs as system role on the reaction (ESM v10), retaining the CLI/Stage realization gap."
  feature Certificates
    slice StateView CertificatesToSend
      description "Work queue: membership means 'still to send'; there is no status flag"
      readmodel PendingCertificate
        enrolmentId EnrolmentId
        completedAt DateTime
      query PendingCertificateById => PendingCertificate optional
        by enrolmentId EnrolmentId
      projection CertificatesToSend => PendingCertificate
        from CoursePassed
          enrolmentId = $eventSourceId
          completedAt = completedAt
        remove with CertificateSent
      specification OpeningAnItem
        given CoursePassed
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          completedAt = "2026-10-05T08:00:00Z"
        then query PendingCertificateById
          arguments
            enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          result
            enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
            completedAt = "2026-10-05T08:00:00Z"
      specification ClosingAnItem
        given CoursePassed
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          completedAt = "2026-10-05T08:00:00Z"
        when append CertificateSent
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          sentAt = "2026-10-05T08:00:00Z"
        then no readmodel PendingCertificate for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
      specification NotQueueingAFail
        when append CourseFailed
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
        then no readmodel PendingCertificate for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"

    slice StateChange SendCertificate
      description "Realization sends the certificate with idempotency key = enrolment id, then appends"
      command SendCertificate
        enrolmentId EnrolmentId identifier
        produces event CertificateSent
          sentAt DateTime = $context.occurred
      constraint IssueOnce
        unique event CertificateSent
      specification SendingACertificate
        given clock "2026-10-05T08:00:00Z"
        when SendCertificate
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
        then CertificateSent
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          sentAt = "2026-10-05T08:00:00Z"
      specification RefusingASecondSend
        given clock "2026-10-05T08:00:00Z"
        given CertificateSent
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          sentAt = "2026-10-05T08:00:00Z"
        when SendCertificate
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
        then error "Constraint 'IssueOnce' is violated: the event source already has the constrained event."

    slice Automation SendCertificates
      reaction CertificateSender
        description "Sends a certificate for every passed course; ends at CertificateSent"
        when CoursePassed
          enrolmentId
          invokes SendCertificate
            enrolmentId = enrolmentId
      specification SendingAfterAPass
        given clock "2026-10-05T08:00:00Z"
        when append CoursePassed
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          enrolmentId = "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          completedAt = "2026-10-05T08:00:00Z"
        then CertificateSent
          for "0d3b9e2a-6c4f-4f1e-8b7a-5e2d1c3b4a01"
          sentAt = "2026-10-05T08:00:00Z"
```

## Design-only additions

Two things a real queue often wants stay out of the complete model above: a list query (the
visible queue) and a retry sweep that reads the whole view. The list query binds (Stage renders snapshot and observable lists); the sweep needs a code body the
reference does not run, and durable per-item fan-out is outside the language. Say so
in the verdict.

Requires Screenplay 4.105.0 or later (`screenplay --version`) for the list query; before it the list query is refused with PLAY0268.

```screenplay excerpt parent=assets/pending-certificates.play
// Parent: the complete document above, slice StateView CertificatesToSend.
query PendingCertificates => PendingCertificate[]
```

```screenplay excerpt parent=assets/retry-sweep.play
// Parent: the complete document above, slice Automation SendCertificates, after CertificateSender.
// The sweep reads the whole view (a clock trigger takes no `by`); iterating items needs code.
reaction CertificateRetrySweep
  description "Retries items still pending; per-item fan-out needs code (#286)"
  every 15 minutes
    reads PendingCertificate
    file Reactions/RetryPendingCertificates.cs
```

`reads` documents what a trigger consults; it never protects (a trigger with `reads` that
`produces` directly fails binding: decide in a command instead).
