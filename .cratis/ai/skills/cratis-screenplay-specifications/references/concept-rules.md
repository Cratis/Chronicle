<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/concept-rules.md -->
## Business rule or concept rule?

Before writing an error specification, sort the rule:

1. Does the error depend on **existing system state** - what events occurred?
   → business rule → write the specification.
2. Would a different business plausibly have a different rule here?
   → business rule → write the specification.
3. Is it a format, range or presence rule on a single value?
   → put it on the **concept** with its own `validate` block, so the rule travels
   with every use, and write **one rejection specification per concept rule**,
   through **one** command that uses the concept. Not one per command or property
   that uses it.

**Write specifications for:** *"Cannot cancel an already-paid invoice"*,
*"Cannot withdraw more than the balance"*, *"Maximum 100 lines per invoice"*, and,
once per concept rule, *"Discount cannot exceed 100 percent"*.

**Do not write** one specification per use of a concept, or one per format variant
of the same rule.

**Why this differs from earlier guidance.** This skill used to say not to write
specifications for rules like *"Name cannot be empty"* because the type system
makes them unrepresentable. In Screenplay a concept `validate` is a runtime rule the
reference runner executes, not a type-level guarantee . Without a
rejection specification nothing proves the rule is wired to the command, and a
rule moved or deleted by an edit goes unnoticed. The cost is one specification per
rule; the rule stays on the concept, never copied into each command.

A complete example: the rule lives on the concept, one command exercises it, and
each concept rule has a rejection (the boundary above the limit and the rule below
zero), plus an acceptance at the limit.

```screenplay
concept InvoiceId : Uuid
concept DiscountPercentage : Int
  validate
    min 0 message "Discount cannot be negative"
    max 100 message "Discount cannot exceed 100 percent"
module Invoicing
  feature Discounts
    slice StateChange GrantDiscount
      command GrantDiscount
        invoiceId InvoiceId identifier
        discount  DiscountPercentage
        produces event DiscountGranted
          discount DiscountPercentage = discount
      specification GrantingAValidDiscount
        when GrantDiscount
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          discount  = 15
        then DiscountGranted
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          discount = 15
      specification AcceptingTheUpperBoundary
        when GrantDiscount
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          discount  = 100
        then DiscountGranted
          for "9c858901-8a57-4791-81fe-4c455b099bc9"
          discount = 100
      specification RejectingADiscountAboveTheUpperBoundary
        when GrantDiscount
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          discount  = 101
        then error "Discount cannot exceed 100 percent"
      specification RejectingANegativeDiscount
        when GrantDiscount
          invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          discount  = -1
        then error "Discount cannot be negative"
```

Which scenarios a command needs (denial, duplicates, competing claims, ordering,
external failure, compensation) and which form fits a mode: coverage-matrix and
workshop method in `cratis-screenplay-scenario-coverage`. This skill stays the
grammar and the reference-execution facts.
