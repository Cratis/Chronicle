<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/response-continuations.md -->
# Returned names in UI continuations

## Contents

- Success scope
- Failure and shadowing
- Renderer boundary
- Compiled example

## Success scope

Screenplay resolves returned names outside the ESM (Requires Screenplay 4.148.0 or later). A scalar
`returns invoiceId` exposes `invoiceId`; a `returns` block exposes its field
names. The form's block `on submit` and one-line
`on submit navigate to <Screen> by <name>` have the form command's response
scope. `execute <Command>` creates that scope only under its `on success`.

Bare-name value operands in `with x from <name>`, form `by <name>`,
`set … to <name>`, and unquoted `notify`/`confirm` messages resolve to the nearest
successful response that supplies that name. Form fields and behavior parameters
win over returned names. Dotted paths, `$` roots, quoted literals and opaque
expressions keep their old meaning. Action declaration names and state-write
targets are not response operands. Screen `action <Command>` navigation sugar
gets no response scope.

## Failure and shadowing

A command's own returned name in its `on failure` is **PLAY0685**: there is no
response on failure. An outer successful response remains available inside an
inner command's failure. Nested execute-success scopes shadow matching outer
names only in that subtree; siblings restore the outer scope. `refresh`,
`confirm`, `open dialog` and `raise` continuations inherit the surrounding scope
without creating a response. A named behavior's parameterized `execute command`
has no known response shape at declaration time.

Returned values still cannot supply another command's generated input
(**PLAY0485**). Generation belongs to the server, not the continuation.
Monaco/VS Code complete returned names after `from` and form `by`; hover shows
the field type. Editor resolution is not runtime execution.

Sources: Cratis/Screenplay `Documentation/screenplay/interactions.md`,
`Source/DotNET/Screenplay/Parsing/SpecificationResponseValidator.cs`,
`Source/DotNET/Screenplay/for_ScreenplayCompiler/when_validating_response_continuations.cs`.

## Renderer boundary

A CLI-bundled compiler can predate this resolution. Stage
returns scalar/named responses through generated TypeScript proxies and passes
accepted responses to Scene's `CommandOutcome.response`, but form response
binding/navigation remains blocked in Scene. Do not infer runtime
continuation support from successful source validation, an unchanged ESM, a
response proxy, or an existing `on success`. Runtime binding remains a renderer
follow-up, not a capability the compiler supplies.

Sources: Cratis/cli `Directory.Packages.props`; Cratis/Stage
`Documentation/reference/generated-values-rendering.md`.

## Compiled example

This authoring example has no form field named `invoiceId`, so the submit `by`
operand denotes the returned identifier. It is not a rendered-browser claim.

```screenplay
concept InvoiceId : Uuid
module Billing
  feature Invoices
    slice StateChange Create
      command CreateInvoice
        invoiceId InvoiceId generated identifier
        title String
        produces event InvoiceCreated
          title String = title
        returns invoiceId
      screen CreateInvoiceScreen
        action CreateInvoice
    slice StateView Details
      readmodel Invoice
        invoiceId InvoiceId
        title String
      projection Invoices => Invoice
        from InvoiceCreated
          invoiceId = $eventSourceId
          title = title
      query InvoiceById => Invoice optional
        by invoiceId InvoiceId
      screen InvoiceDetails
        data Invoice via query InvoiceById by invoiceId
  form CreateInvoiceForm for CreateInvoice
    field title
    on submit navigate to InvoiceDetails by invoiceId
```

This separate source vector deliberately fails: the command's `receipt` is not
available in its own failure branch.

```screenplay expect PLAY0685
module Receipts
  feature Issue
    slice StateChange Send
      command SendReceipt
        value String
        returns
          receipt = value
      screen ReceiptScreen
        on submit
          execute SendReceipt
            on failure
              notify error receipt
```
