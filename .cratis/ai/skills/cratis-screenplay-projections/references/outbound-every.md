<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/outbound-every.md -->
# `from` plus `every` in an outbound event projection

Read this when a projection that targets a public event combines `from` and `every`.

In an outbound Translate slice, events that outbound projections publish count as producers for
routed facts, `every`/`all` scopes and source-type inference. An `every` scope therefore folds
every producible event type, so a projection that combines `from` and `every` over *different*
producible event types is ambiguous and refused: the published event no longer has one distinct
source type. The executable model reports it as `InvalidSemanticContract` ("needs one unambiguous
declared producer destination type") when a routed fact or specification fixture needs that
type; no `PLAY` code is assigned. Keep the producing events to one source type, or drop `every`
and map each event in its own `from`.
