# Projection Declaration Language

The Projection Declaration Language (PDL) is part of the **Cratis Screenplay** language — the
declarative, indentation-based language for describing Cratis applications. Chronicle uses the
Screenplay compiler to parse projection declarations and turn them into projection definitions.

The full language reference — from-event rules, property mapping, auto-map, keys, event context,
counters, arithmetic, joins, children, nested objects, removal, expressions, and the grammar — lives
in the Screenplay documentation:

- [Screenplay — Projections](/screenplay/projections/)

## Read-model identifiers and property names

The target after `=>` is the registered read-model identifier, not its display name or schema
title. For the .NET client, this is the full type name (`typeof(TReadModel).FullName`), such as
`MyApp.ReadModels.Order`. Chronicle renders registered projections with that identifier so you
can save the declaration unchanged.

Identifiers and property paths must match their registered spelling and casing exactly. The
.NET client's default naming policy uses PascalCase properties. A missing name remains a compiler
error; when a title, short type name, or differently cased name matches a known identifier or
property, the diagnostic suggests the registered spelling. Ambiguous matches list the candidates.

## What remains here

Chronicle-specific client APIs that consume PDL declarations are documented in this section:

- [Ad-hoc Querying](adhoc-querying) — running a PDL declaration ad-hoc with `IProjections.Query()`.
