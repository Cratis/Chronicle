// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

/// <summary>An existing instance reports a modification.</summary>
public class and_the_state_changes : given.all_dependencies
{
    ReadModelChangeNotification _result;

    void Establish() => _sink.FindOrDefault(Arg.Any<Concepts.Keys.Key>()).Returns(new ExpandoObject());

    async Task Because() => _result = await _pipeline.Reduce(CreateContext(EventSourceIdValue), CreateReducer(new ExpandoObject()));

    [Fact] void should_report_modified() => _result.Change.ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_serialize_the_stored_state() => _result.ReadModel.ShouldNotBeNull();
}
