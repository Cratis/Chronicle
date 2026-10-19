// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventResultHandler.when_handling;

public class and_the_kernel_lacks_the_capability : given.a_reactor_delivery_with_an_event_log
{
    protected override bool KernelSupportsIdempotentAppends => false;

    Result<ReactorSideEffectFailure> _result;

    async Task Because() => _result = await new EventResultHandler().Handle(_reactorContext, _eventStore, new object());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_unkeyed_once() => _eventLog.ReceivedWithAnyArgs(1).Append(default!, default!, default, default, default, default, default, default, default, default);
    [Fact] void should_not_append_keyed() => _eventLog.DidNotReceiveWithAnyArgs().AppendOnce(default!, default!, default!, default, default, default, default, default, default, default, default);
}
