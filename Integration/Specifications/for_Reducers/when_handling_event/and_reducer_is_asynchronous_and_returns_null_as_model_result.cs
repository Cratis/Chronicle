// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Specifications.for_Reducers.when_handling_event.and_reducer_is_asynchronous_and_returns_null_as_model_result.context;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers.when_handling_event;

[Collection(ChronicleCollection.Name)]
public class and_reducer_is_asynchronous_and_returns_null_as_model_result(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_reducer_able_to_delete<AsynchronousReducerHandlingDeleteAsNull>(chronicleFixture);

    [Fact] void should_produce_read_model_before_deleted_event() => Context.CountAfterFirstEvent.ShouldEqual(1);
    [Fact] void should_delete_read_model_after_deleted_event() => Context.CountAfterSecondEvent.ShouldEqual(0);
}
