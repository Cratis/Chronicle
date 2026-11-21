<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/query-execution.md -->
# What runs for a query

Read this when checking which query forms bind in the executable model, or writing query specifications.

These bind: keyed `=> RM optional`, unfiltered collections,
collections narrowed by caller-supplied `by`, and their `observable` forms.
Caller-supplied equality filters on collections select ESM v14: same-named scalar
primitive/concept/enum properties, same nominal type ignoring optionality, no
`from`, distinct names not repeating `by`. `by` and supplied filters AND together;
optional filters may be omitted, but supplied null is refused. PLAY0268 still
rejects context sources, single-result filters, opaque scope and
a `by` block on a list query (or on a non-optional single result).
Authorization runs before lookup, ANDed with module/feature gates; policies see
only `by`, never filters. Pin denial with `given caller` and `then denied`.
Specs pass `by` and filters by name (PLAY0468 for unknown, repeated or missing
required arguments); collection results match by read model/key regardless of
row order.

A reducer-built view is keyed by event-source id, so a single-instance `by` of another
nominal type warns `PLAY0690`. Rebuild it as a projection keyed on each `from`:
[reducer identity](../../cratis-screenplay-projections/references/reducer-identity.md).
