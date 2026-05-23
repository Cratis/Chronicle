// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

#if DEVELOPMENT
using Cratis.Chronicle.Storage.Sql;
#endif

namespace Cratis.Chronicle.Integration;

public class Specification<TChronicleFixture>(TChronicleFixture fixture) : XUnit.Integration.Specification<TChronicleFixture>(fixture)
    where TChronicleFixture : IChronicleFixture
{
    public override bool AutoDiscoverArtifacts => false;

    /// <inheritdoc/>
    protected override Action<Configuration.IChronicleBuilder>? GetStorageConfigurator(string mongoServer)
    {
        if (ChronicleFixture is not ChronicleFixture configurable ||
            configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess)
        {
            return null;
        }

        // Out-of-process MongoDB: give the in-process Orleans silo its own isolated database
        // so its grain state never conflicts with the OOP container's grain state. Both silos
        // connect to the same mongod (exposed on the host at port 27018), but writing to
        // different databases prevents cross-silo interference.
        if (configurable.Options.StorageProvider == ChronicleStorageProvider.MongoDB)
        {
            var port = configurable.MongoDBContainer.GetMappedPublicPort(27017);
            var server = $"mongodb://localhost:{port}/?directConnection=true";
            var dbName = configurable.InProcessMongoDatabaseName;
            return cb => Setup.MongoDBChronicleBuilderExtensions.WithMongoDB(cb, server, dbName);
        }

        if (configurable.InProcessStorageType is null)
        {
            return null;
        }

        var storageType = configurable.InProcessStorageType;
        var connectionString = configurable.GetInProcessConnectionString();
        var options = new Configuration.ChronicleOptions
        {
            Storage = new Configuration.Storage { Type = storageType, ConnectionDetails = connectionString }
        };

        return chronicleBuilder => Setup.SqlChronicleBuilderExtensions.WithSql(chronicleBuilder, options);
    }

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<string, string?>? GetStorageHostConfiguration(string mongoServer)
    {
        if (ChronicleFixture is not ChronicleFixture configurable ||
            configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess ||
            configurable.Options.StorageProvider == ChronicleStorageProvider.MongoDB ||
            configurable.InProcessStorageType is null)
        {
            return null;
        }

        return new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Storage:Type"] = configurable.InProcessStorageType,
            ["Cratis:Chronicle:Storage:ConnectionDetails"] = configurable.GetInProcessConnectionString()
        };
    }

    /// <inheritdoc/>
    protected override Sinks.SinkTypeId? GetDefaultSinkTypeId()
    {
        if (ChronicleFixture is ChronicleFixture configurable &&
            configurable.Options.Mode == ChronicleRuntimeMode.OutOfProcess &&
            configurable.Options.StorageProvider != ChronicleStorageProvider.MongoDB)
        {
            return Sinks.WellKnownSinkTypes.SQL;
        }

        return null;
    }

    /// <inheritdoc/>
    protected override async Task WipeInProcessStorage()
    {
#if DEVELOPMENT
        var database = Services.GetService<IDatabase>();
        if (database is not null)
        {
            await database.Wipe();
        }
#else
        await Task.CompletedTask;
#endif
    }
}

/// <summary>
/// Represents a non-generic specification using the default <see cref="ChronicleFixture"/>.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleFixture"/>.</param>
public class Specification(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture);
