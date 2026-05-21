// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for the SQL <see cref="Sink"/>.
/// </summary>
/// <remarks>
/// Takes <see cref="IServiceProvider"/> and resolves <see cref="IDatabase"/> lazily on
/// <see cref="CreateFor"/> rather than eagerly via the constructor. This lets the factory
/// be instantiated even in modes where SQL is not the active backend (e.g. MongoDB
/// integration tests load both Storage.MongoDB and Storage.Sql assemblies, and
/// <c>IInstancesOf&lt;ISinkFactory&gt;</c> enumerates every implementation). Construction
/// no longer fails, and the inactive backend's sink type simply never appears in any
/// read model definition that the active backend would process.
/// </remarks>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve <see cref="IDatabase"/> on demand.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between documents and objects.</param>
public class SinkFactory(
    IServiceProvider serviceProvider,
    IExpandoObjectConverter expandoObjectConverter) : ISinkFactory
{
    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        var database = serviceProvider.GetRequiredService<IDatabase>();
        return new Sink(eventStore, @namespace, readModel, database, expandoObjectConverter);
    }
}
