// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Orleans.TestKit;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.given;

public class a_pii_manager : Specification
{
    protected static readonly EventStoreName EventStore = "some-event-store";
    protected static readonly EventStoreName OtherEventStore = "another-event-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "some-namespace";
    protected static readonly EncryptionKeyIdentifier Identifier = "9c1f6a3e-7d24-4b0f-8a51-6e2d3c4b5a70";

    /// <summary>
    /// Key material the store holds for the subject, so that a spec asserting no log message contains it is
    /// scanning for something that genuinely exists in the scenario rather than for a string nobody could have
    /// written.
    /// </summary>
    protected static readonly EncryptionKey Key = new(Encoding.UTF8.GetBytes("the-public-part"), Encoding.UTF8.GetBytes("the-private-part"));

    protected TestKitSilo _silo = new();
    protected IEncryptionKeyStorage _keyStore;
    protected IEncryptionKeyCacheClient _cacheClient;
    protected IStorage _storage;
    protected RecordingLogger<PIIManager> _logger;
    protected PIIManager _manager;

    /// <summary>
    /// Gets the stable one-way binding the log records the subject as, instead of the identifier itself.
    /// </summary>
    protected static string SubjectBinding =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Identifier.Value)));

    async Task Establish()
    {
        _keyStore = Substitute.For<IEncryptionKeyStorage>();
        _cacheClient = Substitute.For<IEncryptionKeyCacheClient>();
        _storage = Substitute.For<IStorage>();
        _logger = new RecordingLogger<PIIManager>();

        _storage.GetEventStores().Returns([EventStore, OtherEventStore]);
        _keyStore.TryGetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Identifier, Arg.Any<EncryptionKeyRevision?>()).Returns(Key);
        _keyStore.GetErasureFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Identifier)
            .Returns(new EncryptionKeyErasure(EncryptionKeyRevision.Initial, [Key.Fingerprint], NewKeyAllowed: false));

        _silo.AddService(_keyStore);
        _silo.AddService(_cacheClient);
        _silo.AddService(_storage);
        _silo.AddService<Microsoft.Extensions.Logging.ILogger<PIIManager>>(_logger);

        _manager = await _silo.CreateGrainAsync<PIIManager>(Guid.Empty, new PIIManagerKey(EventStore, EventStoreNamespace));
    }

    /// <summary>
    /// Nothing derived from the key may reach a log: not the material, not its fingerprint.
    /// </summary>
    protected void ShouldNotHaveLoggedAnyKeyMaterial()
    {
        _logger.Messages.ShouldNotBeEmpty();

        foreach (var message in _logger.Messages)
        {
            message.ShouldNotContain(Key.Fingerprint);
            message.ShouldNotContain(Convert.ToBase64String(Key.Public));
            message.ShouldNotContain(Convert.ToBase64String(Key.Private));
            message.ShouldNotContain(Encoding.UTF8.GetString(Key.Public));
            message.ShouldNotContain(Encoding.UTF8.GetString(Key.Private));
        }
    }
}
