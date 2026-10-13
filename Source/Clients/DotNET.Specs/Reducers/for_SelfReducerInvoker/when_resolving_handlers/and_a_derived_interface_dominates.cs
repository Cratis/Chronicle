// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_SelfReducerInvoker.when_resolving_handlers;

public class and_a_derived_interface_dominates : given.emitted_methods
{
    ReduceResult _result;

    async Task Because()
    {
        var invoker = new SelfReducerInvoker(Registry(typeof(Joined)), typeof(State), "state");
        _result = await invoker.Invoke(Substitute.For<IServiceProvider>(), [new(new Joined(), EventContext.Empty)], null);
    }

    [Fact] void should_choose_the_more_derived_interface() => ((State)_result.ReadModelState!).Handler.ShouldEqual("left");

    public record State(string Handler)
    {
        public static State Reduce(IRoot @event, State? current) => new("root");
        public static State Reduce(ILeft @event, State? current) => new("left");
    }
}
