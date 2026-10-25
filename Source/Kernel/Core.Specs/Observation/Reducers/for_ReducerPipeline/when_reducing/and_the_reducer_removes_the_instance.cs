// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>Removing an existing instance reports its stored state.</summary>
public class and_the_reducer_removes_the_instance : given.all_dependencies
{
    ReadModelChangeNotification _result;

    void Establish() => _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(new ExpandoObject());

    async Task Because() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(null));

    [Fact] void should_report_removed() => _result.Change.ChangeType.ShouldEqual(ReadModelChangeType.Removed);
    [Fact] void should_serialize_the_removed_state() => _result.ReadModel.ShouldNotBeNull();
}
