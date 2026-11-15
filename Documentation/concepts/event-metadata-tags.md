---
title: Cratis Chronicle event metadata tags for stream routing
description: Cratis Chronicle uses source and stream metadata to identify events and scope concurrency checks. Custom tags label events without guarding appends.
---
Event metadata tags in Cratis Chronicle identify which source and stream an event belongs to. Use them to distinguish an account's transactions from its onboarding events, or to check for concurrent changes to a particular stream before appending. These four values sit alongside the event's payload.

## EventSourceType

`EventSourceType` identifies the category of [event source](./event-source.md), such as `Account`. It distinguishes account events from events belonging to orders or shipments.

## EventSourceId

`EventSourceId` identifies the particular source, such as an account number. Events for the same account share this value even when they belong to different processes.

## EventStreamType

`EventStreamType` identifies a process within the source, such as `Onboarding` or `Transactions`. An event source definition declares the stream names you can append through.

## EventStreamId

`EventStreamId` distinguishes stream instances within the same source and stream type. For example, separate IDs can keep monthly transaction streams apart.

Declare the source type and stream types once with [event sources and streams](../event-sources/index.mdx), then append through that definition instead of repeating names at each call site.

All four values can narrow a concurrency scope: the set of events Chronicle checks for changes before accepting an append. This is how Chronicle expresses an append condition for a [Dynamic Consistency Boundary](../dynamic-consistency-boundary/chronicle.md). Custom tags support categorization and filtering, but do not take part in that check.
