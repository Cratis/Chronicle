---
id: 0001-empty-collection-responses
title: Normalize empty collection responses at the contract boundary
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/Kernel/Contracts/**
  - Source/Tools/GrpcCodeGenerator/**
  - Source/Clients/DotNET/**
---

# Normalize empty collection responses at the contract boundary

## Context

PR #4028 addresses absent protobuf collection values reaching client converters as null.
A response-name-only audit misses nested payloads and legacy contracts. Per-API fallbacks
leave every new query exposed to the same failure.

## Decision

Chronicle represents an empty collection response with an empty collection, not null.
Query and command envelopes share payload-default creation. Generated contract collection
members receive mutable defaults, and an assembly-wide specification enforces initialization
on both construction and deserialization for all non-nullable contract collections.
Client converters consume the contract rather than adding null fallbacks for each API.

## Options considered

- **Shared contract defaults and generation:** chosen because every caller benefits and
  specifications can enforce the rule across generated and legacy messages.
- **Per-query or per-converter fallbacks:** rejected because each new API can regress.
- **Shared immutable empty collections:** rejected because protobuf deserialization can
  populate an existing collection, and responses must not share mutable state.

## Default if unanswered

Existing contract defaults and scattered client fallbacks remain inconsistent. Empty results
can still fail in converters or force application code to distinguish null from empty.

## Timeline and scope

Apply before shipping #4028 and retain the rule for subsequent contracts. This covers
collection payloads and non-nullable collection members, including legacy contracts.
It does not reinterpret nullable scalar metadata or change failure, validation, authorization,
or explicitly nullable single-entity lookup semantics. No protobuf field numbers change.

## Verification

**Done when:** supported collection-shaped envelopes and all non-nullable collection members
are non-null when empty; populated collections survive transport without loss; failures remain failures.

**Verify by:** run `DotNET.Specs`, including `for_response_defaults` and `for_grpc_contracts`,
and `GrpcCodeGenerator.Specs`; run the empty-result integration specifications and the
wire-compatibility workflow for #4028.

## Consequences

Applications enumerate successful empty results directly. New contracts cannot introduce
uninitialized collection members without failing the contract-wide specification. Query
failure must still be checked independently of payload presence.
