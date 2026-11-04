// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a method of an observer that takes an <see cref="EventBatch"/>.
/// </summary>
/// <param name="Method">The <see cref="MethodInfo"/> of the method.</param>
/// <param name="EventTypes">The event types the method observes.</param>
internal sealed record BatchMethod(MethodInfo Method, FrozenSet<Type> EventTypes)
{
    /// <summary>
    /// Gets whether the method observes an event type.
    /// </summary>
    /// <param name="eventType">The event <see cref="Type"/>.</param>
    /// <returns>True if it does, false if not.</returns>
    public bool Observes(Type eventType) => EventTypes.Contains(eventType);

    /// <summary>
    /// Resolves the arguments for a call to the method.
    /// </summary>
    /// <param name="leading">The arguments for the leading parameters, in order.</param>
    /// <param name="context">The <see cref="BatchContext"/> of the call.</param>
    /// <param name="services">The <see cref="IServiceProvider"/> for any other parameter.</param>
    /// <returns>The arguments.</returns>
    public object?[] ArgumentsFor(object?[] leading, BatchContext context, IServiceProvider services) =>
        [.. Method.GetParameters().Select((parameter, index) => index < leading.Length ? leading[index] : Resolve(parameter.ParameterType, context, services))];

    static object Resolve(Type type, BatchContext context, IServiceProvider services)
    {
        if (type == typeof(BatchContext))
        {
            return context;
        }

        return type == typeof(CancellationToken) ? context.CancellationToken : services.GetRequiredService(type);
    }
}
