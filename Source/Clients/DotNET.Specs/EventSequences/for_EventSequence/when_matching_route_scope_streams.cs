// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_matching_route_scope_streams : given.a_scope_correctness_context
{
    readonly List<Exception> _refusals = [];
    readonly List<bool> _refusedBeforeAppend = [];
    readonly List<bool> _independentChecks = [];
    int _legacyAppends;
    int _matchingAppends;

    async Task Because()
    {
        var linked = new ConcurrencyScopeBuilder().ForEventSource<EventSources.for_EventSources.ShoppingCartEventSource>("Items")
            .WithEventSourceId(_sourceId).WithSequenceNumber(42UL).Build();
        foreach (var path in new[] { 1, 3, 4 })
        {
            foreach (var scope in new[] { linked, linked with { EventStreamId = "copy" } })
            {
                var before = _appendCalls;
                _refusals.Add(await Catch.Exception(() => AppendOnPath(path, scope)));
                _refusedBeforeAppend.Add(_appendCalls == before);
                _options.AllowRouteScopeStreamMismatch = true;
                await AppendOnPath(path, scope);
                _legacyAppends++;
                _options.AllowRouteScopeStreamMismatch = false;
            }
            await AppendOnPath(path, linked with { EventStreamType = "Payment" });
            await AppendOnPath(path, linked with { DefinitionType = typeof(EventSources.for_EventSources.OrderEventSource) });
            await AppendOnPath(path, new ConcurrencyScope(42UL, _sourceId, "Items"));
            _matchingAppends += 3;
        }
        foreach (var path in new[] { 0, 8, 9, 10 })
        {
            await AppendOnPath(path, linked);
            _independentChecks.Add(_wireScope.SequenceNumber == 42 && _wireScope.EventStreamType == "Items");
        }
    }

    [Fact] void should_refuse_each_routed_api_and_linked_with_copy() => _refusals.ShouldEachConformTo(_ => _ is ConcurrencyScopeStreamDoesNotMatchAppend);
    [Fact] void should_refuse_before_any_append_rpc() => _refusedBeforeAppend.ShouldEachConformTo(_ => _);
    [Fact] void should_allow_each_mismatch_under_the_legacy_switch() => _legacyAppends.ShouldEqual(6);
    [Fact] void should_allow_matching_streams_other_definitions_and_unlinked_scopes() => _matchingAppends.ShouldEqual(9);
    [Fact] void should_keep_labelled_conditions_and_route_less_prepared_events_independent() => _independentChecks.ShouldEachConformTo(_ => _);
    [Fact] void should_exercise_all_refusals() => _refusals.Count.ShouldEqual(6);
}
