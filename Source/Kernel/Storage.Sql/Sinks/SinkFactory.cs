// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for the SQL <see cref="Sink"/>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> for accessing SQL storage.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between documents and objects.</param>
public class SinkFactory(
    IDatabase database,
    IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel) =>
        new Sink(eventStore, @namespace, readModel, database, expandoObjectConverter);
}
