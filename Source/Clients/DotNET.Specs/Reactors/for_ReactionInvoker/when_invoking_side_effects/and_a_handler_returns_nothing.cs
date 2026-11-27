// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects;

public class and_a_handler_returns_nothing : given.a_reactor_with_recorded_side_effects
{
    ReactorInvocationResult _nullSync;
    ReactorInvocationResult _nullAsync;
    ReactorInvocationResult _plainTask;

    async Task Because()
    {
        _nullSync = await Invoke(new NullSyncReactor());
        _nullAsync = await Invoke(new NullAsyncReactor());
        _plainTask = await Invoke(new PlainTaskReactor());
    }

    [Fact] void should_succeed_for_a_null_synchronous_value() => _nullSync.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_for_a_null_awaited_value() => _nullAsync.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_for_a_task_without_a_result() => _plainTask.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_involve_the_side_effect_handlers() => _calls.ShouldEqual(["before", "after", "before", "after", "before", "after"]);

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class NullSyncReactor : IReactor
    {
        public Effect? Handle(Triggered @event) => null;
    }

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class NullAsyncReactor : IReactor
    {
        public Task<Effect?> Handle(Triggered @event) => Task.FromResult<Effect?>(null);
    }

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class PlainTaskReactor : IReactor
    {
        public Task Handle(Triggered @event) => Task.CompletedTask;
    }
}
