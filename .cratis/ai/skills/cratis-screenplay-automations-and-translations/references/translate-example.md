<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/translate-example.md -->
# Example: a capture and its follow-up Automation

## Contents

- Model
- Notes for the reader

A complete model: a webhook record from an outside proctoring service is captured, kept as an
external fact in the provider's vocabulary, then followed by a separate Automation that records our result. It
binds as ESM v6 (`executableReady: true`), standalone and through `cratis screenplay mcp`; a
compiler that stops before v6 compiles it (V1) but rejects the Translate slice at binding
(PLAY0268). `cratis render` refuses it (`STAGE-ESM-016` or `STAGE-ESM-024`, by renderer).

Read the slice `description`: it is the per-field disposition (map, translate, ignore) and it
states what this example does not do. Examples never show the whole integration contract; use
`references/integration-contracts.md` and `references/worked-integration.md` for the rest.

## Model

```screenplay
// Translate slice. Correlation is created on OUR side: BookExam allocates the attempt id and the
// realization hands it to the proctoring service as its reference; the service's webhook echoes
// it, so the capture `key` lands every record on the attempt's own stream. The external fact keeps
// the provider's vocabulary (ProctorResultReceived); translator reactions record our facts.

domain Acme.Academy

concept AttemptId : Uuid
concept MemberId : Uuid
concept ProctorOutcome : Enum
  passed
  failed

policy IsAuthenticated
  require authenticated
persona Member
  description "Takes exams. Cannot: record or change results"
  policy IsAuthenticated

module Exams
  description "Exam attempts, graded by an external proctoring service"
  authorize IsAuthenticated
  feature Attempts
    slice StateChange BookExam
      command BookExam
        attemptId AttemptId identifier
        memberId  MemberId
        produces event ExamBooked
          memberId MemberId = memberId
      specification BookingAnExam
        given caller
          authenticated
        when BookExam
          attemptId = "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          memberId  = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then ExamBooked
          for "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          memberId = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
      specification RefusingAnAnonymousBooking
        given caller
        when BookExam
          attemptId = "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          memberId  = "6f1c2a8e-0b1d-4d55-9a3e-2f6a7c1d0e11"
        then denied

    slice Translate ImportProctorResults
      description "Per external field: reference -> correlation key (our attempt id, echoed); outcome -> translated to our enum; score -> kept; examinerNote -> ignored (free text, may hold personal data, no consumer). Repeated delivery: the first record of a key appends once (`when added`); later records compare outcome only. An unchanged outcome appends nothing, even if the score changed, so it does not implement score corrections. OneResultPerAttempt refuses a second local result. Changed outcomes and score-only corrections need an explicit correction policy (open decision, recorded in STATE.md, not an overwrite) before this is a complete integration."
      capture ProctorResults
        source webhook
          path /webhooks/proctoring/results
        key reference
        map
          outcome = outcome translate
            "P" => passed
            "F" => failed
        append ProctorResultReceived
          when added
            outcome = $.outcome
            score   = $.score
        append ProctorResultReceived
          when outcome
            outcome = $.outcome
            score   = $.score
      event ProctorResultReceived
        outcome ProctorOutcome
        score   Int
      specification TranslatingAPass
        when capture ProctorResults
          reference = "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          outcome   = "P"
          score     = 87
        then ProctorResultReceived
          for "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          outcome = "passed"
          score   = 87
        then ExamPassed
          for "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          score = 87
    slice Automation RecordExamResult
      reaction PassTranslator
        when ProctorResultReceived
          outcome
          score
          produces ExamPassed
            score = score
        where outcome == "passed"
      reaction FailTranslator
        when ProctorResultReceived
          outcome
          produces ExamFailed
        where outcome == "failed"
      event ExamPassed
        score Int
      event ExamFailed
      constraint OneResultPerAttempt
        unique event ExamPassed
        unique event ExamFailed
      specification RefusingASecondResult
        given ExamPassed
          for "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          score = 87
        when append ProctorResultReceived
          for "7e9d1c2b-4a3f-4e5d-8c6b-1a2b3c4d5e06"
          outcome = "failed"
          score   = 40
        then error "Constraint 'OneResultPerAttempt' is violated: the event source already has the constrained event."
```

## Notes for the reader

- Keep the capture and its appended event in Translate; put reactions to that local
  event in a separate Automation. Co-location in a directionless Translate warns
  PLAY0694 and fails `--warnaserror`; explicit inbound refuses it with PLAY0616.
  `then` lists every new fact, so `TranslatingAPass` includes the Automation result.
- `when added` records first sight; the separate `when outcome` append handles
  later changes. For an update spec, seed the same reference with `given capture`.
  Requires Screenplay 4.175.0 or later (`screenplay --version`); older evaluators
  treat a first record's fields as changed.
- The unauthenticated-booking spec needs an explicit `given caller` (empty means no caller);
  an authorized command exercised without one is PLAY0389 at binding.
- Authenticity of the webhook, tenant ownership and a score-correction policy are not in the
  model. They are target requirements and an open decision (see the slice description).
