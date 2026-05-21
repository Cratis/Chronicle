// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage.Sql;

namespace Cratis.Chronicle.Integration;

public class Specification<TChronicleFixture>(TChronicleFixture fixture) : XUnit.Integration.Specification<TChronicleFixture>(fixture)
    where TChronicleFixture : IChronicleFixture
{
    public override bool AutoDiscoverArtifacts => false;

    /// <inheritdoc/>
    protected override Action<Configuration.IChronicleBuilder>? GetStorageConfigurator(string mongoServer)
    {
        if (ChronicleFixture is not ChronicleFixture configurable ||
            configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess ||
            configurable.Options.StorageProvider == ChronicleStorageProvider.MongoDB ||
            configurable.InProcessStorageType is null)
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
    protected override SinkTypeId? GetDefaultSinkTypeId()
    {
        if (ChronicleFixture is ChronicleFixture configurable &&
            configurable.Options.Mode == ChronicleRuntimeMode.OutOfProcess &&
            configurable.Options.StorageProvider != ChronicleStorageProvider.MongoDB)
        {
            return WellKnownSinkTypes.SQL;
        }

        return null;
    }

    /// <inheritdoc/>
    protected override Task ClearStorageMigrationCaches()
    {
        Services.GetService<IDatabase>()?.ClearTableMigrationCache(string.Empty);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a non-generic specification using the default <see cref="ChronicleFixture"/>.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleFixture"/>.</param>
public class Specification(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture);
