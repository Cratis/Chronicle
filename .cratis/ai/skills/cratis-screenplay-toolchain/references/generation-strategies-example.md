<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/generation-strategies-example.md -->
# Generation strategies example (ESM v15)

A deterministic UUIDv7 identifier and NanoID receipt, with an accepted response
and a reused-source rejection. Fixtures, not random seeds, make this executable.
Source contract: Cratis/Screenplay `v4.148.0:Documentation/screenplay/generated-values.md:30-60`;
strategy encodings: `generated-values/strategies.md:34-89`.

```screenplay
concept InvoiceId : Uuid
  generate uuidv7
concept Receipt : String
  generate nanoid
module Billing
  feature Drafts
    slice StateChange Start
      command StartDraft
        invoiceId InvoiceId generated identifier
        receipt Receipt generated
        produces event DraftStarted
          receipt Receipt = receipt
        returns invoiceId
      specification StartsWithPinnedValues
        when StartDraft
          for "00112233-4455-7677-8899-aabbccddeeff"
          generated receipt = "abcdefghijklmnopqrstu"
        then DraftStarted
          for "00112233-4455-7677-8899-aabbccddeeff"
          receipt = "abcdefghijklmnopqrstu"
        then returns "00112233-4455-7677-8899-aabbccddeeff"
      specification RejectsAUsedGeneratedSource
        given DraftStarted
          for "00112233-4455-7677-8899-aabbccddeeff"
          receipt = "abcdefghijklmnopqrstu"
        when StartDraft
          for "00112233-4455-7677-8899-aabbccddeeff"
          generated receipt = "abcdefghijklmnopqrstu"
        then error "A generated event source already has facts; retry the command with a fresh generated value."
```

The rejection appends no new facts and returns no response. v15 guards the whole
source even if the old fact uses another stream or event type. Removing every
serialized generation strategy restores the earlier Unguarded contract, not an
alternative way to make a stale-source specification pass.
