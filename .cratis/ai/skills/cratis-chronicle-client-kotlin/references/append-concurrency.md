<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/append-concurrency.md -->
# Append concurrency

This reference holds how to narrow a concurrency scope for an idempotent create.

For an idempotent create, narrow the concurrency scope and use
`ConcurrencyScopeBuilder.withExpectsNoMatchingEvent()`. The equivalent is
`ConcurrencyScope(EventSequenceNumber.unavailable, eventSourceId = true,
expectsNoMatchingEvent = true)`; a concrete sequence number and this flag are
mutually exclusive (package `eventSequences.concurrency`).
