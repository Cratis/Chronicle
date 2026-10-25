// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines a factory that can create instances of <see cref="ISink"/> for a specific <see cref="SinkTypeId"/>.
/// </summary>
public interface ISinkFactory
{
    /// <summary>
    /// Gets the <see cref="SinkTypeId"/> that identifies the type of store the factory supports.
    /// </summary>
    SinkTypeId TypeId { get; }

    /// <summary>
    /// Create a <see cref="ISink"/> for a specific <see cref="ReadModelDefinition"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the sink is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the sink is for.</param>
    /// <param name="readModel"><see cref="ReadModelDefinition"/> to create for.</param>
    /// <returns>A new instance of <see cref="ISink"/> for the <see cref="ReadModelDefinition"/>.</returns>
    ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel);

    /// <summary>
    /// Creates a generation writer from immutable execution and a fixed grant, or refuses before allocation.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="context">The generation execution and admission.</param>
    /// <returns>The generation sink.</returns>
    /// <exception cref="Generations.Bindings.MaterializationBindingsNotSupported">This provider cannot create generation writers.</exception>
    ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, GenerationSinkContext context) =>
        throw new Generations.Bindings.MaterializationBindingsNotSupported();
}
