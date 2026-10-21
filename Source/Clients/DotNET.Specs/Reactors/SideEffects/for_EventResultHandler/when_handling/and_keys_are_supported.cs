// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventResultHandler.when_handling;

public class and_keys_are_supported : given.a_reactor_delivery_with_an_event_log
{
    Result<ReactorSideEffectFailure> _result;
    IdempotencyKey _key;

    void Establish() => _eventLog.AppendOnce(default!, default!, default!, default, default, default, default, default, default, default, default)
        .ReturnsForAnyArgs(callInfo =>
        {
            _key = callInfo.ArgAt<IdempotencyKey>(2);
            return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
        });

    async Task Because() => _result = await new EventResultHandler().Handle(_reactorContext, _eventStore, new object());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_once_with_the_key_of_index_zero() => _key.ShouldEqual(_delivery.ReturnedEffectKey(0));
    [Fact] void should_not_append_unkeyed() => _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!, default, default, default, default, default, default, default, default);
}
