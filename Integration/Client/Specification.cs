// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using KernelConcepts = Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Integration;

public class Specification<TChronicleFixture>(TChronicleFixture fixture) : Cratis.Chronicle.XUnit.Integration.Specification<TChronicleFixture>(fixture)
    where TChronicleFixture : IChronicleFixture
{
    public override bool AutoDiscoverArtifacts => false;

    /// <inheritdoc/>
    protected override Action<Cratis.Chronicle.Configuration.IChronicleBuilder>? GetStorageConfigurator(string mongoServer)
    {
        if (ChronicleFixture is not ChronicleConfigurableFixture configurable ||
            configurable.Options.Mode != ChronicleRuntimeMode.OutOfProcess ||
            configurable.Options.StorageProvider == ChronicleStorageProvider.MongoDB ||
            configurable.InProcessStorageType is null)
        {
            return null;
        }

        var storageType = configurable.InProcessStorageType;
        var connectionString = configurable.GetInProcessConnectionString();
        var options = new Cratis.Chronicle.Configuration.ChronicleOptions
        {
            Storage = new Cratis.Chronicle.Configuration.Storage { Type = storageType, ConnectionDetails = connectionString }
        };

        return chronicleBuilder => Cratis.Chronicle.Setup.SqlChronicleBuilderExtensions.WithSql(chronicleBuilder, options);
    }

    /// <inheritdoc/>
    protected override IReadOnlyDictionary<string, string?>? GetStorageHostConfiguration(string mongoServer)
    {
        if (ChronicleFixture is not ChronicleConfigurableFixture configurable ||
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
        if (ChronicleFixture is ChronicleConfigurableFixture configurable &&
            configurable.Options.Mode == ChronicleRuntimeMode.OutOfProcess &&
            configurable.Options.StorageProvider != ChronicleStorageProvider.MongoDB)
        {
            return WellKnownSinkTypes.SQL;
        }

        return null;
    }

    public IEventStoreStorage EventStoreStorage =>
        Services.GetRequiredService<IStorage>().GetEventStore(Constants.EventStore);

    public IEventStoreNamespaceStorage GetEventStoreNamespaceStorage(KernelConcepts.EventStoreNamespaceName? namespaceName = null) =>
        EventStoreStorage.GetNamespace(namespaceName ?? KernelConcepts.EventStoreNamespaceName.Default);

    public IEventSequenceStorage GetEventLogStorage(KernelConcepts.EventStoreNamespaceName? namespaceName = null) =>
        GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(KernelConcepts.EventSequences.EventSequenceId.Log);

    public IEventSequenceStorage GetSystemEventLogStorage(KernelConcepts.EventStoreNamespaceName? namespaceName = null) =>
        GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(KernelConcepts.EventSequences.EventSequenceId.System);
}

/// <summary>
/// Represents a non-generic specification using the default <see cref="ChronicleConfigurableFixture"/>.
/// </summary>
/// <param name="fixture">The <see cref="ChronicleConfigurableFixture"/>.</param>
public class Specification(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture);
