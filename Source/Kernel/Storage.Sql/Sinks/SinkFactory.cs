// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for the SQL <see cref="Sink"/>.
/// </summary>
/// <remarks>
/// Marked with <see cref="IgnoreConventionAttribute"/> so convention binding does not register it
/// automatically. The SQL sink factory depends on SQL-specific services that are only wired up by
/// <c>WithSql</c>; when both Storage.MongoDB and Storage.Sql assemblies are loaded (as in the
/// integration test project), letting convention binding pick up backend-specific factories
/// causes <c>IInstancesOf&lt;T&gt;</c> enumerations to fail for the inactive backend.
/// </remarks>
/// <param name="database">The <see cref="IDatabase"/> for accessing SQL storage.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between documents and objects.</param>
[IgnoreConvention]
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
