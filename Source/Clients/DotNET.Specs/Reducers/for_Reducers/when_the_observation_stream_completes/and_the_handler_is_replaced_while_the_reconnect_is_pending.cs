// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_the_observation_stream_completes;

public class and_the_handler_is_replaced_while_the_reconnect_is_pending : given.a_reducer_whose_stream_completes
{
    protected override Func<TimeSpan, CancellationToken, Task> WaitBeforeReconnect => (_, _) =>
    {
        var replacement = Substitute.For<IReducerHandler>();
        replacement.Id.Returns((ReducerId)"completing-reducer");
        replacement.ReducerType.Returns(typeof(CompletingReducer));
        replacement.ReadModelType.Returns(typeof(ReadModel));
        _handlersByModelType[typeof(ReadModel)] = replacement;
        return Task.CompletedTask;
    };

    async Task Because() => await CompleteTheStreamAndRunTheCapturedWork();

    [Fact] void should_not_observe_again() => _registrationsAfterCompletion.ShouldEqual(0);
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
