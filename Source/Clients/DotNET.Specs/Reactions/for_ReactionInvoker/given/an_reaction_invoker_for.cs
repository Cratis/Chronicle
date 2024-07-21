// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactions.for_ObserverInvoker.given;

public class an_reaction_invoker_for<TReaction> : Specification
{
    protected Mock<IServiceProvider> service_provider;
    protected IEventTypes event_types;
    protected Mock<IReactionMiddlewares> middlewares;
    protected ReactionInvoker invoker;

    void Establish()
    {
        service_provider = new();
        event_types = new EventTypesForSpecifications(GetEventTypes());
        middlewares = new();

        invoker = new ReactionInvoker(
            service_provider.Object,
            event_types,
            middlewares.Object,
            typeof(TReaction),
            Mock.Of<ILogger<ReactionInvoker>>());
    }

    protected virtual IEnumerable<Type> GetEventTypes() => [];
}
