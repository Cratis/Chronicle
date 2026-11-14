---
id: 0031-typed-graph-views
title: Read property graphs through typed, finite views that compile to a backend-neutral query, executed by host packages
status: accepted
stage: implemented
class: contract
reversibility: reversible
proposed: 2026-11-14
decided: 2026-11-14
decider: session owner (cross-repository graph views request)
applies-to:
  - Source/Clients/DotNET/Graphs/**
  - Source/Clients/DotNET/Projections/Graphs/GraphProjections.cs
  - Source/Clients/DotNET/IClientArtifactsProvider.cs
  - Source/Clients/Testing/Graphs/**
  - Source/Clients/DotNET.CodeAnalysis/Analyzers/GraphViewAnalyzer.cs
---

<!-- markdownlint-disable-next-line MD025 -->
# Read property graphs through typed, finite views that compile to a backend-neutral query, executed by host packages

## Context

Graph projections write events to a property graph of nodes and edges, but a consumer had no typed way to read it
back. Reading meant hand-written Cypher against the store, with no check that the labels, edge types and properties
it names exist, and no in-memory way to specify the read. Every Chronicle client and Screenplay share one
specification of graph views, with a conformance fixture for the declaration and shared query vectors for the
results, so every implementation must give the same definitions and the same results.

## Decision

1. **The schema layer is untouched.** Nodes, edges, `GraphDefinition`, the graph projections, the five sink operations,
   the gRPC contracts, the kernel and its grains do not change. Views are additive client surface: a graph that
   declares no views behaves and registers byte for byte as before.
2. **A view is a read model per consumer.** A view is a record anchored at one node (`ViewOf<TNode>`). Its members are
   node properties, relations along declared edges to other views (`Follows<TEdge>`), or properties of the followed
   edge (`FromEdge`). Each consumer declares the view it needs instead of sharing one shape.
3. **Nesting is finite.** A view cannot contain itself, directly or transitively; recursion, variable-length paths,
   aggregation, `or`, ranges and ordering other than key order are out of scope.
4. **Declarations are checked against the graph** with the thirteen shared reason codes, each its own exception type.
   A view that breaks a rule is reported through the same failure mechanism graph registration uses and is left out;
   the other views and the graphs are unaffected. Four of the rules are also analyzers (CHR0078-CHR0081).
5. **Queries compile to a backend-neutral AST.** Clauses are member selectors only, never free predicates, so nothing
   can fail to translate at runtime. A `GraphQuery` carries a resolved tree of the view, so an executor needs no other
   information.
6. **Reads execute through host packages.** `IGraphQueryExecutor` and the neutral `GraphRow` tree are the stable
   contract a host implements; the Neo4j executor lives in Arc. The client ships the reference in-memory executor in
   `GraphScenario`, held to the shared query vectors.

## Options considered

- **Query the graph with free LINQ predicates.** Rejected: a predicate the translator cannot express compiles and fails
  at runtime, the failure mode the house conventions forbid.
- **Add view definitions to the wire and register them with the kernel.** Rejected: views only read; the kernel has no
  use for them, and changing the wire would turn an additive feature into a protocol change.
- **Put the Cypher translation in the client.** Rejected: the client does not reach the graph store; the host that
  holds the connection, its tenancy and namespace rules owns the translation.

## Default if unanswered

Not applicable: decided.

## Timeline and scope

Delivered with the graph views change in the .NET client and its testing package. The other clients follow the same
specification in their own repositories.

## Verification

- The Organization views serialize byte for byte to the shared conformance fixture, in both forms.
- One specification per reason code.
- The shared query vectors run against `GraphScenario` and every case matches exactly.

## Consequences

- Reads are eventually consistent with the event log and may see stale elements while a projection replays.
- A host package must implement `IGraphQueryExecutor` to give applications a working `IGraph`; until one is used, views
  can be declared, validated and specified in memory but not read from a store.
