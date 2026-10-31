// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class with_a_reducer : given.a_collection_stream
{
    void Establish()
    {
        _projections.HasFor(Arg.Is<Type>(type => type == typeof(Row))).Returns(false);
        _reducers.HasFor(Arg.Is<Type>(type => type == typeof(Row))).Returns(true);
    }

    async Task Because()
    {
        _observation = _readModels.Observe<Row>();
        Subscribe();
        await Emit(new Row("one", "Reduced"));
    }

    [Fact] void should_emit_the_materialized_reducer_set() => _emissions.Single().ShouldContainOnly(new Row("one", "Reduced"));
    [Fact] void should_not_use_reducer_watchers() => _reducerObservers.ReceivedCalls().ShouldBeEmpty();
}
