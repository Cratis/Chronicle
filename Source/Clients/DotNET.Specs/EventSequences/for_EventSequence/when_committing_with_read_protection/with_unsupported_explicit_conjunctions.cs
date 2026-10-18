// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_unsupported_explicit_conjunctions : given.a_trusted_commit
{
    void Establish() => ((IKernelCapabilities)_connection).Capabilities.Returns([]);

    Task Because() => Execute(async () =>
    {
        _unit.AddEvents(
            EventSequenceId.Log,
            [new("source", "event")],
            [new("source", new ConcurrencyScope(40, "source", "first")), new("source", new ConcurrencyScope(40, "source", "second"))]);
        await Commit();
    });

    [Fact] void should_refuse_the_unsupported_conjunction() => _error.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_not_report_resolved_conditions() => _unit.GetAppendConditions().ShouldBeEmpty();
    [Fact] void should_not_send_the_request() => _request.ShouldBeNull();
}
