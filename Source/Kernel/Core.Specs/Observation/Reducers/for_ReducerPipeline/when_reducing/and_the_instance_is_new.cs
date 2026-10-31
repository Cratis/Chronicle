// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>A newly stored instance can be watched.</summary>
public class and_the_instance_is_new : given.all_dependencies
{
    ReadModelChangeNotification _result;
    ExpandoObject _returnedState;
    bool _written;
    bool _writtenBeforeNotification;

    void Establish()
    {
        _returnedState = new ExpandoObject();
        ((IDictionary<string, object?>)_returnedState)["value"] = "after";
        _objectComparer.Compare(Arg.Any<ExpandoObject>(), Arg.Any<ExpandoObject>(), out Arg.Any<IEnumerable<Changes.PropertyDifference>>()).Returns(call =>
        {
            call[2] = new Changes.PropertyDifference[] { new("value", null, "after") };
            return false;
        });
        _sink.ApplyChanges(Arg.Any<Concepts.Keys.Key>(), Arg.Any<Changes.IChangeset<Concepts.Events.AppendedEvent, ExpandoObject>>(), Arg.Any<Concepts.Events.EventSequenceNumber>(), Arg.Any<Storage.Sinks.SinkWriteMode>()).Returns(_ =>
        {
            _written = true;
            return Task.FromResult<IEnumerable<Storage.Sinks.FailedPartition>>([]);
        });
    }

    async Task Because()
    {
        _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(_returnedState));
        _writtenBeforeNotification = _written;
    }

    [Fact] void should_report_added() => _result.Change.ChangeType.ShouldEqual(ReadModelChangeType.Added);
    [Fact] void should_report_the_key() => _result.ReadModelKey.ShouldEqual((ReadModelKey)EventSourceIdValue);
    [Fact] void should_report_the_last_event() => _result.Change.EventSequenceNumber.ShouldEqual(Concepts.Events.EventSequenceNumber.First);
    [Fact] void should_write_before_returning_the_notification() => _writtenBeforeNotification.ShouldBeTrue();
}
