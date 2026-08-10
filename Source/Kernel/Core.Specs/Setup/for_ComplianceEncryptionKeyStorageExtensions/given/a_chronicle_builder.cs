// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ComplianceOptions = Cratis.Chronicle.Configuration.Compliance;
using EncryptionOptions = Cratis.Chronicle.Configuration.Encryption;
using KeyStorageOptions = Cratis.Chronicle.Configuration.Storage;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions.given;

public class a_chronicle_builder : Specification
{
    protected ServiceCollection _services;
    protected IChronicleBuilder _builder;
    protected InMemoryEncryptionKeyStorage _defaultStorage;
    protected InMemoryEncryptionKeyStorage _dedicatedStorage;
    protected EncryptionKeyIdentifier _identifier;
    protected EncryptionKey _keyOnlyInTheDefaultStorage;
    ServiceProvider _serviceProvider;

    void Establish()
    {
        _services = [];
        _services.AddLogging();
        _builder = new ChronicleBuilder(Substitute.For<ISiloBuilder>(), _services, Substitute.For<IConfiguration>());
        _defaultStorage = new InMemoryEncryptionKeyStorage();
        _dedicatedStorage = new InMemoryEncryptionKeyStorage();
        _identifier = new EncryptionKeyIdentifier(Guid.NewGuid().ToString());
        _keyOnlyInTheDefaultStorage = new EncryptionKey(Encoding.UTF8.GetBytes("default-public"), Encoding.UTF8.GetBytes("default-private"));
    }

    void Destroy() => _serviceProvider?.Dispose();

    protected static ChronicleOptions OptionsMigratingFromTheDefaultStorage(bool migrate) => new()
    {
        Compliance = new ComplianceOptions
        {
            Encryption = new EncryptionOptions
            {
                Storage = new KeyStorageOptions { Type = "vault", ConnectionDetails = "http://vault:8200" },
                MigrateFromDefaultStorage = migrate
            }
        }
    };

    protected int RegistrationsOfEncryptionKeyStorage() =>
        _services.Count(_ => _.ServiceType == typeof(IEncryptionKeyStorage));

    protected IChronicleBuilder RegisterTheDedicatedStorage(bool migrate) =>
        _builder.WithComplianceEncryptionKeyStorage(OptionsMigratingFromTheDefaultStorage(migrate), _ => _dedicatedStorage);

    protected IEncryptionKeyStorage Resolve()
    {
        _serviceProvider = _services.BuildServiceProvider();
        return _serviceProvider.GetRequiredService<IEncryptionKeyStorage>();
    }

    protected Task<EncryptionKey?> KeyIn(IEncryptionKeyStorage store) =>
        store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    protected Task SeedTheDefaultStorage() =>
        _defaultStorage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _keyOnlyInTheDefaultStorage);
}
