// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorHandler.when_handling_two_deliveries;

public class with_an_inherited_causation : given.all_dependencies
{
    const string Request = "Request";

    CausationManager _manager;
    ReactorHandler _handler;
    EventContext _eventContext;
    List<IImmutableList<Causation>> _capturedChains;
    IImmutableList<Causation> _outerChain;
    ReactorInvocationResult _firstResult;
    ReactorInvocationResult _secondResult;

    void Establish()
    {
        _manager = new();
        _handler = new(_eventStore, _reactorId, typeof(object), _eventSequenceId, [], _manager, _identityProvider);
        _eventContext = EventContext.Empty with
        {
            EventType = new(Guid.NewGuid().ToString(), 1),
            SequenceNumber = 42
        };
        _capturedChains = [];
        _reactorInvoker.Invoke(Arg.Any<object>(), Arg.Any<EventContext>()).Returns(_ =>
        {
            _capturedChains.Add(_manager.GetCurrentChain());
            return Task.FromResult(ReactorInvocationResult.Success());
        });
    }

    async Task Because()
    {
        _manager.Add(Request, new Dictionary<string, string>());
        _firstResult = await _handler.OnNext(_eventContext, new SomeEvent("First"), _reactorInvoker);
        _secondResult = await _handler.OnNext(_eventContext with { SequenceNumber = 43 }, new SomeEvent("Second"), _reactorInvoker);
        _outerChain = _manager.GetCurrentChain();
    }

    void Destroy() => _handler.Dispose();

    [Fact] void should_handle_the_first_delivery_successfully() => _firstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_handle_the_second_delivery_successfully() => _secondResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_capture_both_deliveries() => _capturedChains.Count.ShouldEqual(2);
    [Fact] void should_include_exactly_one_reactor_causation_in_the_first_delivery() => _capturedChains[0].Count(_ => _.Type == ReactorHandler.CausationType).ShouldEqual(1);
    [Fact] void should_include_exactly_one_reactor_causation_in_the_second_delivery() => _capturedChains[1].Count(_ => _.Type == ReactorHandler.CausationType).ShouldEqual(1);
    [Fact] void should_keep_the_inherited_causation_in_the_first_delivery() => _capturedChains[0][1].Type.Value.ShouldEqual(Request);
    [Fact] void should_keep_the_inherited_causation_in_the_second_delivery() => _capturedChains[1][1].Type.Value.ShouldEqual(Request);
    [Fact] void should_not_leak_reactor_causation_into_the_outer_flow() => _outerChain.Any(_ => _.Type == ReactorHandler.CausationType).ShouldBeFalse();
    [Fact] void should_leave_only_the_root_and_inherited_causation_in_the_outer_flow() => _outerChain.Count.ShouldEqual(2);
}
