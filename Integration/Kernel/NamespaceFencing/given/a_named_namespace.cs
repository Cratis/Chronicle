// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;

using Client = global::Cratis.Chronicle;
using KernelConfiguration = KernelCore::Cratis.Chronicle.Configuration;
using NativeEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using NativeNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;
using NativeStoreName = Cratis.Chronicle.Concepts.EventStoreName;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing.given;

/// <summary>Uses real kernel/client/storage paths. Constructing or compiling this context must never run it.</summary>
/// <param name="fixture">The explicitly integration-only fixture.</param>
public abstract class a_named_namespace(NamespaceFenceFixture fixture) : Cratis.Chronicle.XUnit.Integration.Specification<NamespaceFenceFixture>(fixture)
{
    protected const string Tenant = "tenant-a";

    protected readonly string _storeName = $"fence-{Guid.NewGuid():N}";
    protected Client.IEventStore _store = null!;
    protected CapturedWriter _oldWriter = null!;
    protected Client.Transactions.IUnitOfWork _oldUnit = null!;
    protected INamespaceIncarnations _owners = null!;
    protected IHaveNamespaceIncarnations _capability = null!;
    protected IChronicleServicesAccessor _services = null!;
    protected Guid _oldIdentity;
    readonly Lazy<string> _sqlDirectory = new(fixture.CreateSqliteDirectory);

    protected virtual bool UseSqlite => false;
    public override bool AutoDiscoverArtifacts => false;
    public override IEnumerable<Type> EventTypes => [typeof(NamespaceFenceValue)];

    /// <inheritdoc/>
    public override void Dispose()
    {
        DisposeUnit();
        base.Dispose();
    }

    protected static bool IsFenceRefusal(Exception error)
    {
        if (error is not CommandFailed failed) return false;
        var known = new[]
        {
            Contracts.Namespaces.NamespaceErrorCodes.IncarnationMismatch,
            Contracts.Namespaces.NamespaceErrorCodes.IncarnationClosed,
            Contracts.Namespaces.NamespaceErrorCodes.IncarnationRequiresReactivation
        };
        return failed.Result?.ExceptionMessages.Intersect(known, StringComparer.Ordinal).Any() == true;
    }

    protected override Action<KernelConfiguration.IChronicleBuilder>? GetStorageConfigurator(string mongoServer) => builder =>
    {
        if (UseSqlite)
        {
            builder.WithSql(new KernelConfiguration.ChronicleOptions { Storage = new KernelConfiguration.Storage { Type = "Sqlite", ConnectionDetails = SqliteConnectionString() } });
        }
        else
        {
            builder.WithMongoDB(mongoServer, Constants.EventStore);
        }
        builder.SiloBuilder.ConfigureServices(services => services.PostConfigure<KernelConfiguration.ChronicleOptions>(options =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:NamespaceIncarnations"] = "true"
            }).Build().Bind(options)));
    };

    protected override IReadOnlyDictionary<string, string?>? GetStorageHostConfiguration(string mongoServer) => UseSqlite
        ? new Dictionary<string, string?>
        {
            ["Cratis:Chronicle:Storage:Type"] = "Sqlite",
            ["Cratis:Chronicle:Storage:ConnectionDetails"] = SqliteConnectionString()
        }
        : base.GetStorageHostConfiguration(mongoServer);

    protected override Client.Sinks.SinkTypeId? GetDefaultSinkTypeId() => UseSqlite ? Client.Sinks.WellKnownSinkTypes.SQL : null;

    protected async Task Recreate()
    {
        await _owners.Remove(_storeName, Tenant);
        var removed = await ((Client.Namespaces.INamespaceBindings)_store).LookupNamespaceBinding(Tenant);
        removed.Status.ShouldEqual(Client.Namespaces.NamespaceBindingStatus.Removed);
        removed.Incarnation.ShouldBeNull();
        await _owners.Ensure(_storeName, Tenant);
    }

    protected async Task<CapturedWriter> CurrentWriter()
    {
        var captured = await Capture();
        captured.Identity.ShouldNotEqual(_oldIdentity);
        return captured.Writer;
    }

    protected async Task<ulong> StoredCount() => (await Services.GetRequiredService<IStorage>().GetEventStore(_storeName)
        .GetNamespace(Tenant).GetEventSequence(NativeEventSequenceId.Log).GetCount()).Value;

    protected Task<Contracts.Namespaces.NamespaceRemovalStatusResponse> Status() => _services.Services.Namespaces.GetNamespaceRemovalStatus(new()
    {
        EventStore = _storeName,
        Namespace = Tenant
    }).EnsureSuccess();

    protected async Task<ulong> NamespaceNotifications() => (await Services.GetRequiredService<IStorage>().GetEventStore(NativeStoreName.System)
        .GetNamespace(NativeNamespaceName.Default).GetEventSequence(NativeEventSequenceId.System)
        .GetCountMatching(new(EventSourceId: $"{_storeName}/{Tenant}"))).Value;

    async Task Establish()
    {
        _store = await Services.GetRequiredService<Client.IChronicleClient>().GetEventStore(_storeName, Tenant);
        await _store.RegisterAll();
        _owners = Services.GetRequiredService<INamespaceIncarnations>();
        _capability = _owners.GetStorage(_storeName);
        _services = (IChronicleServicesAccessor)_store.Connection;
        var captured = await Capture();
        _oldIdentity = captured.Identity;
        _oldWriter = captured.Writer;
        _oldUnit = new Client.Transactions.UnitOfWork(CorrelationId.New(), _ => { }, _store);
        captured.Bind(_oldUnit);
    }

    async Task<(CapturedWriter Writer, Guid Identity, Action<Client.Transactions.IUnitOfWork> Bind)> Capture()
    {
        var binding = await ((Client.Namespaces.INamespaceBindings)_store).LookupNamespaceBinding(Tenant);
        binding.Status.ShouldEqual(Client.Namespaces.NamespaceBindingStatus.Open);
        var sequence = ((Client.Namespaces.INamespaceBoundEventSequences)_store).GetEventSequence(Client.EventSequences.EventSequenceId.Log, binding);
        var writer = new CapturedWriter(
            async (source, value) => (await sequence.Append(source, value)).IsSuccess,
            async (source, value, key) =>
            {
                var result = await sequence.AppendOnce(source, value, new Client.EventSequences.IdempotencyKey(key));
                return (result.IsSuccess, result.WasAlreadyAppended);
            });
        return (writer, binding.Incarnation!.Value, unit => ((Client.Transactions.INamespaceBoundUnitOfWork)unit).BindNamespace(binding));
    }

    string SqliteConnectionString() => $"Data Source={Path.Combine(_sqlDirectory.Value, "chronicle.db")};Pooling=False";

    void DisposeUnit()
    {
        _oldUnit?.Dispose();
        _oldUnit = null!;
    }

    void Destroy() => DisposeUnit();
}
