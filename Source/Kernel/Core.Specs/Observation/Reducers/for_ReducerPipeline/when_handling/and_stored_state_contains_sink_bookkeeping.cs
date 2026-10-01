// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

public class and_stored_state_contains_sink_bookkeeping : given.all_dependencies
{
    ExpandoObject _initial;
    PropertyDifference[] _differences;

    void Establish()
    {
        _initial = new ExpandoObject();
        var values = (IDictionary<string, object?>)_initial;
        values["count"] = 1;
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        values[WellKnownProperties.LastHandledEventSequenceNumber] = 1UL;
        values[WellKnownProperties.Subject] = EventSourceIdValue;
        values[WellKnownProperties.Subjects] = new ExpandoObject();
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(_initial);
        _sink.ApplyChanges(Arg.Any<Concepts.Keys.Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>())
            .Returns(call =>
            {
                _differences = call.ArgAt<IChangeset<AppendedEvent, ExpandoObject>>(1).Changes
                    .OfType<PropertiesChanged<ExpandoObject>>().SelectMany(change => change.Differences).ToArray();
                return Task.FromResult(Enumerable.Empty<FailedPartition>());
            });
        _pipeline = new ReducerPipeline(_readModelDefinition, _sink, new ObjectComparer(), new ReadModelsCompliance(_complianceManager, _expandoObjectConverter), EventStore, EventStoreNamespace);
    }

    async Task Because()
    {
        dynamic state = new ExpandoObject();
        state.count = 2;
        await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(state));
    }

    [Fact] void should_not_remove_sink_bookkeeping() => _differences.Where(difference => WellKnownProperties.All.Contains(difference.PropertyPath.Path) && difference.Changed is null).ShouldBeEmpty();
    [Fact] void should_change_the_reducer_property() => _differences.Single(difference => difference.PropertyPath.Path == "count").Changed.ShouldEqual(2);
    [Fact] void should_not_mutate_the_stored_initial_state() => WellKnownProperties.All.All(((IDictionary<string, object?>)_initial).ContainsKey).ShouldBeTrue();
}
