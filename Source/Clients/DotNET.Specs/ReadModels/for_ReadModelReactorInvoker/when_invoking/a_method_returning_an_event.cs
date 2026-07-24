// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorInvoker.when_invoking;

public class a_method_returning_an_event : given.an_invoker
{
    WatchedReadModel _model;

    void Establish()
    {
        _sideEffectHandlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<object>()).Returns(true);
        _sideEffectHandlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>())
            .Returns(Result.Success<ReactorSideEffectFailure>());
    }

    async Task Because()
    {
        _model = new WatchedReadModel();
        await _invoker.Invoke(_eventStore, _reactor, MethodFor(ReadModelChangeType.Removed), _model, _changeContext, _serviceProvider);
    }

    [Fact] void should_dispatch_the_returned_event_to_the_side_effect_handlers() => _sideEffectHandlers.Received(1).Handle(Arg.Any<ReactorContext>(), _eventStore, _reactor.ReturnedEvent!);
}
