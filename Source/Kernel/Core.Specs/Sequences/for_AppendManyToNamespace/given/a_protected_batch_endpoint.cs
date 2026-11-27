// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyToNamespace.given;

public class a_protected_batch_endpoint : Sequences.given.a_namespace_bound_batch_endpoint
{
    protected AppendManyToNamespace _command;

    void Establish() => _command = new(
        "store",
        "tenant",
        _captured,
        "event-log",
        "source",
        [new(new("event", 1, false), "{}", [new("account", "one")], "subject")],
        Tags: ["batch-tag"]);

    protected async Task Append() => _error = await Catch.Exception(() => _command.Handle(_grainFactory, _causation, _principal, _incarnations));
}
