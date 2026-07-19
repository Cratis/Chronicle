# Projection Declaration Language

The Projection Declaration Language (PDL) is part of the **Cratis Screenplay** language — the
declarative, indentation-based language for describing Cratis applications. Chronicle uses the
Screenplay compiler to parse projection declarations and turn them into projection definitions.

The full language reference — from-event rules, property mapping, auto-map, keys, event context,
counters, arithmetic, joins, children, nested objects, removal, expressions, and the grammar — lives
in the Screenplay documentation:

- [Screenplay — Projections](/screenplay/projections/)

## What remains here

Chronicle-specific client APIs that consume PDL declarations are documented in this section:

- [Ad-hoc Querying](adhoc-querying) — running a PDL declaration ad-hoc with `IProjections.Query()`.
