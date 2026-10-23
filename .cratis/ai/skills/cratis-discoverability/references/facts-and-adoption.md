<!-- cratis-ai-managed: skills/cratis-discoverability/references/facts-and-adoption.md -->
# Fact blocks and adoption numbers

## At a glance

1. Identify the entity the page defines. Add one visible "at a glance" block per entity page,
   near its definition, not repeated promotional blocks throughout the page.
2. Select only verifiable facts: what it is, license, public status, supported capabilities and
   approved public company/founder facts. Attach a source to every fact; counts need both a
   source and an as-of month. A date alone is not evidence.
3. Check the source supports the exact wording. Distinguish vendor claims from independent
   measurements; repository creation is not a founding date, and contributors are not employees.
4. Omit anything unsupported. Never invent customers, user counts, benchmarks, certifications,
   prices or competitor comparisons. Do not turn an unpublished term into a public promise.

A minimal block can say `License: MIT ([license](<public source URL>))` after checking that
source. Add a download row only after the calculation below. Keep the dated calculation and
source links in the owning project's evidence, with one maintained source of truth for reused
claims. Shared corpus guidance must not copy factual counts, legal entity details or names of
private/unannounced integrations from that evidence.

## Counting downloads

1. Work per product, registry and era. List the user-installed entry packages or server images,
   their public registry sources, observation month and counter meaning. Count entry packages
   only: exclude sub-packages, add-ons and starter/meta-packages that repeat those installs.
2. For a product continuation, include predecessor package names and explain the lineage from
   public evidence. Distinct historical entry distributions may count; do not include an
   unrelated product simply because it shares an organization or dependency.
3. Record registry measurements and gaps: NuGet displayed totals may be rounded, npm totals
   need complete time ranges, Docker Hub reports pulls, and Hex reports downloads. Do not
   silently replace missing or incomplete measurements with estimates or claim an unmeasured
   registry. These are downloads/pulls, not unique adoption.
4. Sum the included entry counters once per product. Do not add shared dependencies to a
   company headline: their downloads overlap consuming products. Avoid a cross-product total
   unless its exclusions and overlap are documented; per-product claims are the default.
5. Choose a conservative threshold below the supported total and round down, never to nearest
   or up. Publish "more than <rounded threshold> downloads as of <month year>, across <named
   registries>, including <public predecessor names>". For Docker, explain that pulls are part
   of the combined measure. Never relabel downloads as users, customers, teams or installations.
6. Recheck the calculation and source completeness before publishing. Keep one maintained
   calculation for reuse; never change its date without refreshing the measurements. If evidence is incomplete, omit the claim and report the gap.

## Verification

A pass has one visible block per entity page, a working source for every fact, a date on every
count, and a reproducible download calculation listing included entries, excluded dependencies,
predecessors, registry coverage and rounding threshold. Compare visible wording and JSON-LD
with those sources. No unsupported claim or private detail should remain; report missing
sources rather than filling gaps.
