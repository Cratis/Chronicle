// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>Unchanged state must not notify watchers.</summary>
public class and_nothing_changes : given.all_dependencies
{
    ReadModelChangeNotification _result;

    void Establish()
    {
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(new ExpandoObject());
        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<Changes.PropertyDifference>>()).Returns(true);
    }

    async Task Because() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(new ExpandoObject()));

    [Fact] void should_not_report_a_change() => _result.ShouldBeNull();
    [Fact] void should_not_write_the_sink() => _sink.DidNotReceive().ApplyChanges(Arg.Any<Concepts.Keys.Key>(), Arg.Any<Changes.IChangeset<Concepts.Events.AppendedEvent, ExpandoObject>>(), Arg.Any<Concepts.Events.EventSequenceNumber>(), Arg.Any<Storage.Sinks.SinkWriteMode>());
}
