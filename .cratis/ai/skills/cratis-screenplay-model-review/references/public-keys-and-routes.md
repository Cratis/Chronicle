<!-- cratis-ai-managed: skills/cratis-screenplay-model-review/references/public-keys-and-routes.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Public contracts, lookup keys and observer routes

Add these to the element sweep, with evidence pinned to the declaration:

- Does a command publish a public event, a foreign event get produced locally,
  or a local public event get produced outside an explicit outbound Translate?
  Check for unintended personal/secret fields in the exported contract. An outbound
  slice owns exactly one local public event type and consumes private local facts.
- Is direction explicit when public metadata is used? Does an inbound translation
  accept public foreign events and emit private local facts? A quoted origin is
  an opaque store name with an unverified local shape, not proof of interoperability.
  Read automations-and-translations `references/public-events-and-direction.md`.
- Is the view identity explicitly marked `key` or unambiguously inferred? Does each
  single-instance query/read supply every part exactly once, with compatible types,
  and do state fixtures contain all parts? Do PDL keys align with the view's key?
  Composite view keys still refuse binding (PLAY0268); compilation
  is not executable evidence. Read read-surface `references/named-keys.md`.
- Does a production override replace the whole source/stream/id route, rather than
  assume partial inheritance? Is its `for` destination still explicit and compatible?
  Are skipped production routes valid, so eager resolution cannot fail unexpectedly?
- Does a reaction/reducer `from Source[.Stream]` include the intended producers'
  effective routes? Unrouted facts never match, including capture, reaction-direct
  facts and public publication. PLAY0666 is a C# warning about all *known* producers,
  not proof that a handler/foreign source will deliver. Filters have no stream-id
  predicate and are not projection filters. Read toolchain
  `references/production-routes-and-filters.md`.

Report binding, reference execution and Stage admission separately. Never remove
public metadata, key parts or filters to make a review's mode gate pass.
