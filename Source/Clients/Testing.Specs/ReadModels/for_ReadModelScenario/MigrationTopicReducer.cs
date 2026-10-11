// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Reduces topic events for testing generation delivery to reducers.
/// </summary>
public class MigrationTopicReducer : IReducerFor<ReducedMigrationTopic>
{
    /// <summary>
    /// Gets the reducer identity.
    /// </summary>
    public ReducerId Id => "migration-topic-reducer";

    /// <summary>
    /// Applies the delivered module.
    /// </summary>
    /// <param name="event">The topic event.</param>
    /// <param name="current">The current state.</param>
    /// <returns>The new state.</returns>
    public ReducedMigrationTopic Reduce(MigrationTopicCreated @event, ReducedMigrationTopic? current) => new(@event.Module);
}
