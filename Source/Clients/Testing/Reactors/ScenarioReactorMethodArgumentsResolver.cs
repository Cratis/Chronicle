// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Decorates argument resolution to reject unintentionally absent read models in class-based reactor scenarios.
/// </summary>
/// <param name="inner">The production arguments resolver.</param>
/// <param name="absentReadModels">The read model types and keys declared deliberately absent.</param>
internal sealed class ScenarioReactorMethodArgumentsResolver(
    IReactorMethodArgumentsResolver inner,
    IReadOnlySet<(Type ReadModelType, string Key)> absentReadModels) : IReactorMethodArgumentsResolver
{
    /// <inheritdoc/>
    public async Task<object?[]> Resolve(
        MethodInfo method,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider)
    {
        var arguments = await inner.Resolve(method, reactor, @event, eventContext, eventStore, serviceProvider);
        if (eventStore is not EventStoreForTesting)
        {
            return arguments;
        }

        var parameters = method.GetParameters();
        for (var index = 1; index < parameters.Length; index++)
        {
            var type = parameters[index].ParameterType;
            if (arguments[index] is not null || !(eventStore.Projections.HasFor(type) || eventStore.Reducers.HasFor(type)))
            {
                continue;
            }

            var key = reactor is ICanResolveReadModelKey keyResolver
                ? keyResolver.Resolve(@event, eventContext)
                : (ReadModelKey)eventContext.EventSourceId;
            if (!absentReadModels.Contains((type, key.Value)))
            {
                var readModels = (ReadModelsForTesting)eventStore.ReadModels;
                throw new ReadModelNotSeededForReactorScenario(type, key, [.. readModels.GetSeededKeys(type)]);
            }
        }

        return arguments;
    }
}
