# Empty results and query failures

A successful collection query with no matches returns an empty collection, not `null`.
The same rule applies to non-nullable collection members inside response messages.
Client code should be able to enumerate those collections without a null check.

## Normalize at the contract boundary

Protobuf does not encode a distinction between a missing repeated field and an empty one.
Your generated contracts and result envelopes must expose both as empty collections.
Do not add separate null fallbacks to every query or converter: that leaves each new API
responsible for rediscovering the same transport behavior.

Chronicle's .NET query and command envelopes share empty-payload creation. The contract
generator initializes collection members, and a contract-wide specification checks both
constructed messages and messages deserialized from empty payloads. Legacy, non-generated
messages must satisfy the same specification.

Collection defaults are independent per response. They must also support population by
the serializer: an enumerable interface backed by an immutable empty array can fail when
the deserializer tries to add received elements.

## Empty does not mean successful

Inspect the query result's success, authorization, validation, and exception information
before interpreting its data. A failed query can carry an initialized empty payload; that
payload must not turn the failure into a successful query with no matches.

Likewise, an initialized single-entity response object does not prove that an entity exists.
Honor the lookup API's documented absence semantics. This collection rule does not replace
meaningful nullable scalar metadata, such as an optional state unavailable from an older server.

## Verify more than the empty case

Verify empty and populated responses, nested collections, and failed operations. Populated
values must survive serialization unchanged, and mutating one response's collection must
not change another response. A contract change that only passes the empty case can still
break every response containing data.
