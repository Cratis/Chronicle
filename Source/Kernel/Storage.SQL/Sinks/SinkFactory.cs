// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.SQL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.SQL.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/> for SQL databases.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SinkFactory"/> class.
/// </remarks>
/// <param name="dbContextFactory">Factory for creating <see cref="DbContext"/> instances.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting objects.</param>
/// <param name="sqlOptions">The SQL storage options.</param>
public class SinkFactory(
    IDbContextFactory<ProjectionDbContext> dbContextFactory,
    IExpandoObjectConverter expandoObjectConverter,
    IOptions<SqlStorageOptions> sqlOptions) : ISinkFactory
{
    readonly SqlStorageOptions _options = sqlOptions.Value;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.SQL;

    /// <inheritdoc/>
    public ISink CreateFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model)
    {
        var dbContext = dbContextFactory.CreateDbContext();

        var schemaGenerator = new SqlSchemaGenerator(
            _options.ProviderType,
            GetSchemaName(eventStore, @namespace));

        var changesetConverter = new ChangesetConverter(
            model,
            expandoObjectConverter);

        return new Sink(
            model,
            dbContext,
            changesetConverter,
            expandoObjectConverter,
            schemaGenerator);
    }

    string GetSchemaName(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (_options.UseSchemaForNamespacing)
        {
            // Use namespace for schema separation
            return @namespace.Value.Replace('-', '_').Replace('.', '_');
        }

        return _options.Schema;
    }
}