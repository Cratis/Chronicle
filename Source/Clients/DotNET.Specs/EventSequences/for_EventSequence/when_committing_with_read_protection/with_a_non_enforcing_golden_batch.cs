// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_non_enforcing_golden_batch : given.a_trusted_commit
{
    string _legacyArguments;
    string _currentArguments;

    async Task Because()
    {
        var causation = Causation.Unknown();
        var scope = new ConcurrencyScope(100, "source");

        // The pre-Enforce batch: legacy single-event staging groups by source, while missing scopes
        // still resolve through today's strategy. This is the golden AppendMany input, not an Enforce plan.
        await _sequence.AppendMany(
            [
                new("source", "first", causation),
                new("source", "third", causation),
                new("other", "second", causation)
            ],
            concurrencyScopes: new Dictionary<EventSourceId, ConcurrencyScope>
            {
                ["source"] = scope,
                ["other"] = ConcurrencyScope.NotSet
            });
        _legacyArguments = JsonSerializer.Serialize(_request);

        _unit.AddEventWithOptions(EventSequenceId.Log, "source", "first", causation, new()
        {
            Origin = ConcurrencyScopeOrigin.Aggregate,
            ConcurrencyScope = scope,
            DeclaredConcurrency = ConcurrencyDimensions.EventStreamType
        });
        _unit.AddEventWithOptions(EventSequenceId.Log, "other", "second", causation, new()
        {
            Origin = ConcurrencyScopeOrigin.Routing,
            DeclaredConcurrency = ConcurrencyDimensions.EventStreamId
        });
        _unit.AddEvent(EventSequenceId.Log, "source", "third", causation, concurrencyScope: scope);
        await _unit.Commit();
        _currentArguments = JsonSerializer.Serialize(_request);
    }

    [Fact] void should_send_byte_identical_append_many_arguments() => _currentArguments.ShouldEqual(_legacyArguments);
    [Fact] void should_not_compute_a_read_protection_plan() => _unit.GetAppendConditions().ShouldBeEmpty();
}
