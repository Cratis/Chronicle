// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Keeps the storage, discovered artifacts and services for one read-model fold together.
/// </summary>
/// <param name="EventStore">The store owning event storage, the selected log and migrators.</param>
/// <param name="EventTypes">The registry used to deliver events.</param>
/// <param name="SchemaGenerator">The schema generator used to materialize the read model.</param>
/// <param name="ServiceProvider">The services used to invoke reducers.</param>
/// <param name="ReadModelType">The runtime read model type, including for an untyped fold.</param>
internal sealed record ReadModelFoldContext(
    EventStoreForTesting EventStore,
    IEventTypes EventTypes,
    IJsonSchemaGenerator SchemaGenerator,
    IServiceProvider ServiceProvider,
    Type ReadModelType)
{
    /// <summary>
    /// Gets the original contexts when folding events from the store's log rather than synthetic input.
    /// </summary>
    public IReadOnlyList<EventContext>? EventContexts { get; init; }
}
