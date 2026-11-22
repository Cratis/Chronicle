<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/agent-exposure.md -->
# Explicit agent exposure

Requires Screenplay 6.15.0 (`screenplay --version`); agent declarations bind as ESM v48. On older tools upgrade or stop without emitting or inventing exposure. The product release, ESM version and [manifest contract version](capability-manifest.md) are independent; check the CLI-bundled compiler separately.

## Declare the allowlist

Agent exposure is default-deny. Public operations, screen visibility and ordinary `authorize` declarations do not opt operations in. Declare one application-root `agents` block with exactly one `tokens personal`, then list each operation with an explicit scope and a nonempty literal quoted description.

This complete model exposes one query and one command; `Unlisted` stays unexposed:

```screenplay expect PLAY0976
concept InvoiceId : Uuid
policy Staff
  require role "Staff"
module Sales
  authorize Staff
  feature Invoices
    slice StateChange Creating
      command Create
        invoiceId InvoiceId identifier
      screen CreateInvoice
        action Create for screen
    feature History
      slice StateView Listing
        readmodel Invoice
          invoiceId InvoiceId
        query All => Invoice[]
        query Unlisted => Invoice[]
agents
  tokens personal
  query Sales.Invoices.History.Listing.All
    scope read
    description "List invoices"
  command Sales.Invoices.Creating.Create
    scope write
    description "Create an invoice"
```

Run `screenplay <model-folder> --warnaserror --no-color`; expect exit 0 and no errors or warnings. PLAY0976 information reports the unlisted query, not implicit exposure. Export separately using [capability manifest](capability-manifest.md).

Short references resolve against operations of the declared kind; qualify ambiguous names. Prefer the full address above. The exported canonical identity preserves every enclosing feature, not the short reference you happened to write.

A `scope read` requirement accepts a personal token with read **or** write scope for a query. A query may instead require `scope write`; commands require write scope. The allowlist does not replace effective module, feature and operation authorization. Missing effective authorization warns PLAY0980; keep the policies rather than removing them to obtain binding or export.

An absent block exposes nothing and manifest export refuses `NoAgentsBlock`. An explicitly empty allowlist (`agents` with only `tokens personal`) warns PLAY0977 and exports `operations: []`. A warnings-as-errors validation gate can reject that warning; manifest emission refuses compilation errors, not all warnings. An export is not proof that runtime authorization works.

## Refusals and diagnostics

| Declaration or condition | Result |
| --- | --- |
| Invalid syntax or nonliteral description | PLAY0964 error |
| More than one `agents` block | PLAY0965 error |
| Unknown reference or wrong operation kind | PLAY0966 error |
| Ambiguous reference | PLAY0967 error; qualify it |
| Duplicate resolved operation | PLAY0968 error, even with different short references |
| Missing or blank description | PLAY0969 error |
| Missing, invalid or duplicate scope | PLAY0970 error |
| Command with `scope read` | PLAY0971 error |
| Missing or duplicate tokens declaration | PLAY0972 error |
| OAuth or another unsupported token kind | PLAY0973 error |
| `approval required` | PLAY0974 error; approval is not supported |
| `approve` under an operation | PLAY0964 error: unknown directive, not PLAY0974 |
| Block-global scope | PLAY0975 error; scope belongs on each operation |
| Unlisted operations | PLAY0976 information; aggregate notice, not exposure |
| Explicitly empty allowlist | PLAY0977 warning |
| PLAY0978 / PLAY0979 | Reserved; not emitted newly-exposed/widened verdicts |
| No effective authorization | PLAY0980 warning |
| Exposed global query | PLAY0981 error |
| Exposed performed or graph query | PLAY0982 error |

For example, these are invalid variations, not standalone models. The first requires personal tokens; the second requires write scope and a description:

```screenplay excerpt
agents
  tokens oauth
```

```screenplay excerpt
command Sales.Invoices.Creating.Create
  scope read
```

Do not add `approve` or `approval required` to a valid sample. A description over 1024 Unicode code points is a manifest-only refusal, not a general parser limit; never truncate it to make export pass.

## Review exposure changes

Automatic exposure comparison is not shipped. Use the model review skill's [manual exposure review](../../cratis-screenplay-model-review/references/baseline-comparison.md#manual-agent-exposure-review) for additions and possible widening. Ordinary `semantic-diff` is useful model comparison, not an agent-exposure safety verdict; incomplete or failing compilation remains unresolved.

Read [capability manifest](capability-manifest.md) when exporting or consuming the portable allowlist. Read the owning [capability-is-not-authority rule](../../../rules/capability-is-not-authority.md) before acting on any exposed capability.
