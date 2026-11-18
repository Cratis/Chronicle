<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/concepts-and-types.md -->
# `concept` and `type`

Read this when declaring a concept (validation, enum, protection) or a composite `type`.

```screenplay
concept InvoiceId : Uuid
concept DiscountPercentage : Decimal
  validate
    >= 0    message "A discount cannot be negative"
    <= 100  message "A discount cannot exceed 100 percent"
concept PersonName : String pii
  pii reason "Identifies the billing contact."
concept InvoiceStatus : Enum
  draft
  sent
  paid
```

Primitives: `Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`.
`Enum` is a separate concept kind; details: [enum numbers](enum-numbers.md).
Use bare `pii` (`personal` alias) and `secret` on concepts; **Compliance is inherited**.
Keep protection even when a compiler or renderer refuses it. Read
[compliance and purposes](compliance.md) for ESM v13, binder-only refusals,
consumer ceilings, marker repairs, reasons, scopes and provider limits.

**Identifiers cannot be personal data or secrets** (PLAY0515): see
[compliance](compliance.md#identify-the-person-not-the-stream).

Read [enum numbers](enum-numbers.md) for `draft = 1`, unique int32
all-or-none pins, stored/public identity safety, `enum-pinning`, ESM v12 and the
CLI/Stage gap. Unnumbered enums keep positions; specifications use names.

⚠️ **Enum trap.** A value literally named `validate` is read as an empty validate
block. Write `@validate` for the value; the compiler warns when it sees the
ambiguity.

Use `type <Name>` for a composite shape (several properties) that events and
commands reference; use `concept` for a single wrapped primitive.
