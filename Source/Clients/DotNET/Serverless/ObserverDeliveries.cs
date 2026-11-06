// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Resolves how the events of an observer are delivered.
/// </summary>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the serverless settings.</param>
internal sealed class ObserverDeliveries(ChronicleOptions options)
{
    /// <summary>
    /// Resolves the delivery of an observer.
    /// </summary>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <param name="observerType">The CLR type of the observer, if it has one.</param>
    /// <returns>The resolved <see cref="ObserverDelivery"/>.</returns>
    /// <remarks>
    /// The configuration override wins over <see cref="DeliveryAttribute"/>, which wins over the mode default.
    /// </remarks>
    internal ObserverDelivery Resolve(string observerId, Type? observerType)
    {
        if (options.Serverless.Observers.TryGetValue(observerId, out var observerOptions) && observerOptions.Delivery is { } configured)
        {
            return configured;
        }

        if (observerType?.GetCustomAttribute<DeliveryAttribute>() is { } attribute)
        {
            return attribute.Delivery;
        }

        return ServerlessActivation.IsOn(options) ? options.Serverless.DefaultDelivery : ObserverDelivery.Stream;
    }

    /// <summary>
    /// Gets the override keys that match none of the known observers.
    /// </summary>
    /// <param name="knownObserverIds">The identifiers of the observers that exist.</param>
    /// <returns>The override keys that match no observer.</returns>
    internal IReadOnlyList<string> UnknownOverrides(IEnumerable<string> knownObserverIds)
    {
        var known = new HashSet<string>(knownObserverIds, StringComparer.Ordinal);
        return options.Serverless.Observers.Keys.Where(_ => !known.Contains(_)).ToArray();
    }
}
