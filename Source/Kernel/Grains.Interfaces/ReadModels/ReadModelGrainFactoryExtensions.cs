// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Extension methods for <see cref="IGrainFactory"/> for getting read model grains.
/// </summary>
public static class ReadModelGrainFactoryExtensions
{
    /// <summary>
    /// Get the read models manager for the specified event store.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="eventStoreName">The name of the event store.</param>
    /// <returns>An instance of <see cref="IReadModelsManager"/>.</returns>
    public static IReadModelsManager GetReadModelsManager(this IGrainFactory grainFactory, EventStoreName eventStoreName) =>
        grainFactory.GetGrain<IReadModelsManager>(eventStoreName);


    /// <summary>
    /// Get a specific read model by its name and event store.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="readModelName">The name of the read model.</param>
    /// <param name="eventStoreName">The name of the event store.</param>
    /// <returns>An instance of <see cref="IReadModel"/>.</returns>
    public static IReadModel GetReadModel(this IGrainFactory grainFactory, ReadModelName readModelName, EventStoreName eventStoreName) =>
        grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(readModelName, eventStoreName));
}
