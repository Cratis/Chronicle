// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_a_reducer_without_methods_explicitly : given.all_dependencies
{
    IReducerHandler _handler;
    readonly List<RegisterReducer> _registrations = [];

    void Establish() => _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
    {
        call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _registrations.Add(message.Content.Value0!));
        return Observable.Empty<ReduceOperationMessage>();
    });

    async Task Because() => _handler = await _reducers.Register<EmptyReducer, Model>();

    [Fact] void should_not_register_with_the_kernel() => _registrations.ShouldBeEmpty();
    [Fact] void should_keep_the_client_handler_available() => _reducers.GetHandlerFor<EmptyReducer>().ShouldEqual(_handler);

    public record Model;
    public class EmptyReducer : IReducerFor<Model>;
}
