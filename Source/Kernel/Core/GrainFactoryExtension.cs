// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle;

/// <summary>
/// Extension methods for <see cref="IGrainFactory"/>.
/// </summary>
public static class GrainFactoryExtension
{
    /// <summary>
    /// Gets the <see cref="IRecommendationsManager"/> for a given <see cref="EventStoreAndNamespace"/>.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="eventStoreAndNamespace">The <see cref="EventStoreAndNamespace"/>.</param>
    /// <returns>The <see cref="IRecommendationsManager"/>.</returns>
    public static IRecommendationsManager GetRecommendationsManager(
        this IGrainFactory grainFactory,
        EventStoreAndNamespace eventStoreAndNamespace) =>
        grainFactory.GetGrain<IRecommendationsManager>(
            0,
            new RecommendationsManagerKey(eventStoreAndNamespace.EventStore, eventStoreAndNamespace.Namespace));
}
