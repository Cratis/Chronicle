// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>When nothing is watching, the state is still written but no notification - a whole new copy of the state - is built.</summary>
public class and_nothing_is_watching : given.all_dependencies
{
    ReadModelChangeNotification _result;
    ExpandoObject _returnedState;

    void Establish()
    {
        var initial = new ExpandoObject();
        ((IDictionary<string, object?>)initial)["value"] = "before";
        _returnedState = new ExpandoObject();
        ((IDictionary<string, object?>)_returnedState)["value"] = "after";
        _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(initial);
        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<Changes.PropertyDifference>>()).Returns(call =>
        {
            call[2] = new Changes.PropertyDifference[] { new("value", "before", "after") };
            return false;
        });
    }

    async Task Because() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(_returnedState), createNotification: false);

    [Fact] void should_not_build_a_notification() => _result.ShouldBeNull();
    [Fact] void should_still_write_the_sink() => _sink.Received(1).ApplyChanges(Arg.Any<Concepts.Keys.Key>(), Arg.Any<Changes.IChangeset<Concepts.Events.AppendedEvent, ExpandoObject>>(), Arg.Any<Concepts.Events.EventSequenceNumber>(), Arg.Any<Storage.Sinks.SinkWriteMode>());
}
