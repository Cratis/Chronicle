// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesToNamespace.given;

public class a_protected_batch_endpoint : Sequences.given.a_namespace_bound_batch_endpoint
{
    protected AppendManyForEventSourcesToNamespace _command;
    protected DateTimeOffset _occurred;

    void Establish()
    {
        _occurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _command = new(
            "store",
            "tenant",
            _captured,
            "event-log",
            [new("source", "source-type", "stream-type", "stream", new("event", 1, false), "{}", ["event-tag"], [new("account", "one")], _occurred, "subject", [new(_occurred, "event-cause", new Dictionary<string, string>())], "definition")],
            Tags: ["batch-tag"],
            ConcurrencyScopes: [new("source", new(1, true)), new("source", new(2, true, EventStreamId: "stream"))]);
    }

    protected async Task Append() => _error = await Catch.Exception(() => _command.Handle(_grainFactory, _causation, _principal, _incarnations));
}
