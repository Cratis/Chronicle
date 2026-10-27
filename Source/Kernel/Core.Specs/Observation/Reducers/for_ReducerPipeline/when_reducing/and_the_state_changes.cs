// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>An existing instance reports a modification.</summary>
public class and_the_state_changes : given.all_dependencies
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

    async Task Because() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(_returnedState));

    [Fact] void should_report_modified() => _result.Change.ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_serialize_the_stored_state() => _result.ReadModel.ShouldNotBeNull();
}
