// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_EventStreamReadModel;

public class when_the_declaration_is_invalid : given.stream_models
{
    [Fact] void should_refuse_an_undeclared_stream() => Catch.Exception(() => _modelBound.Build(typeof(UndeclaredStream))).ShouldBeOfExactType<InvalidEventStreamReadModel>();
    [Fact] void should_refuse_all_streams() => Catch.Exception(() => _modelBound.Build(typeof(AllStreams))).ShouldBeOfExactType<InvalidEventStreamReadModel>();
    [Fact] void should_refuse_reducers() => Catch.Exception(() => EventStreamReadModel.RefuseReducer(typeof(MonthlyWithdrawals))).ShouldBeOfExactType<InvalidEventStreamReadModel>();
    [Fact] void should_refuse_a_stream_key()
    {
        var builder = Fluent<MonthlyWithdrawals>();
        builder.From<MoneyWithdrawn>(from => from.UsingKeyFromContext(context => context.EventStreamId));
        Catch.Exception(() => builder.Build()).ShouldBeOfExactType<InvalidEventStreamReadModel>();
    }
}
