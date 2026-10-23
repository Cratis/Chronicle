<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/outcome-comparison.md -->
# How outcomes are compared

This reference holds the comparison rules for events, read models, query rows and rejections.

## How outcomes are compared

- **Events:** the complete set of new events, in authored order, unless
  `then events in any order` is stated. The count is always exact. After
  `when append`, a model before ESM v6 expects the appended fact itself; from
  ESM v6 the appended event is the action and `then` lists only what followed it.
- **Read models and query rows:** only the asserted properties must match
  (subset); add `exactly` to require every property. Query row count is exact;
  collection rows match by read model/key regardless of order. Ordering within a
  collection-valued property is unchanged. A missing property does not match an asserted `null`.
- **Rejections:** `then error "<message>"` matches the message whatever the rule's
  validation severity; no form asserts severity. Bare `then error` never matches
  a denial. Quote a localized key: `then error "$strings.invoices.reasonRequired"`.
  Besides a String `case.<parameter>`, the grammar takes only quoted messages here, never an unquoted `$strings` token
  (unlike a rule's `message` operand). The reference runner compares the symbolic key
  and requires the rejection to carry the string-key marker; it never loads
  translated text; resolving the key is the realization's job.

Date/DateTime `then` matching stays ordinal: different offsets naming one instant
do not match merely because a v26 command comparison considers them equal. Keep
the exact produced payload text in the expectation. Keys, constraints and freshness
also retain ordinal equality. For UTC-tick comparisons and pre-rule missing-offset
Validation rejections, read the [retention-window specifications](../../cratis-screenplay-command-surface/references/date-comparisons.md).

## Rejections and denials say different things

`then error "<message>"` says **rejected, for this reason** — pin it when the
message is the point. Bare `then error` says **rejected, for a reason this
specification does not name**; the reason lives in the specification's name.
`then denied` says **this caller may not do this**, which is decided before any
validation runs. Write the bare form rather than `then error ""`.

**Views cannot reject events.** A projection never refuses an event; there are no
error cases for one. (A `when append` can still be refused by an append-time
constraint — that is the constraint speaking, not the view.)
