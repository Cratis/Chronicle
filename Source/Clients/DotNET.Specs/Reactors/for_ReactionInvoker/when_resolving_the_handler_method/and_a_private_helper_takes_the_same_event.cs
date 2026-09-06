// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_resolving_the_handler_method;

/// <summary>
/// Extracting a helper out of a handler is an ordinary refactor, and the helper naturally takes the event as
/// its first parameter - which makes it a dispatch candidate too. The public handler has to keep the event
/// type, or the reactor's side effects silently stop happening with nothing thrown anywhere.
/// </summary>
public class and_a_private_helper_takes_the_same_event : Specification
{
    ReactorWithAPrivateHelper _reactor;
    ReactorInvoker _invoker;

    void Establish()
    {
        _reactor = new ReactorWithAPrivateHelper();
        _invoker = new ReactorInvoker(
            new EventTypesForSpecifications([typeof(MyEvent)]),
            Substitute.For<IReactorMiddlewares>(),
            typeof(ReactorWithAPrivateHelper),
            new ActivatedArtifact(_reactor, typeof(ReactorWithAPrivateHelper), Substitute.For<ILogger<ActivatedArtifact>>()),
            Substitute.For<ILogger<ReactorInvoker>>());
    }

    async Task Because() => await _invoker.Invoke(new MyEvent(), EventContext.Empty);

    [Fact] void should_call_the_public_handler() => _reactor.HandlerCalls.ShouldEqual(1);
    [Fact] void should_reach_the_helper_only_through_the_handler() => _reactor.HelperCalls.ShouldEqual(1);

    class ReactorWithAPrivateHelper : IReactor
    {
        public int HandlerCalls { get; private set; }

        public int HelperCalls { get; private set; }

        public async Task On(MyEvent @event, EventContext context)
        {
            HandlerCalls++;
            await Enrich(@event);
        }

        /// <summary>
        /// No access modifier, so private - and its first parameter is the same event type the handler takes.
        /// </summary>
        /// <param name="event">The event being handled.</param>
        /// <returns>The enrichment the handler asked for.</returns>
        Task<string> Enrich(MyEvent @event)
        {
            HelperCalls++;
            return Task.FromResult(nameof(Enrich));
        }
    }
}
