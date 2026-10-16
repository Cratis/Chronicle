<!-- cratis-ai-managed: skills/cratis-arc-command/references/event-source-id-details.md -->
# Event source id details

This reference holds the `[Key]` attribute collision and the positional-record and source-type rules for event source id resolution.

`[Key]` on a positional record's primary-constructor parameter is recognized
and counted once. Child-only/nested read-model types are not standalone
command-scoped models. `[EventSource<TSource>(stream)]` supplies source type and
stream metadata on commands and aggregate roots.

Two attributes are spelled `[Key]`. Chronicle reads `Cratis.Chronicle.Keys.KeyAttribute`.
Marking `System.ComponentModel.DataAnnotations.KeyAttribute` in an application
that uses Chronicle compiles, resolves nothing, and silently invents a new event
source id for every command — `ARCCHR0008` reports it.
