// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyFenceProtocol;

public class when_publishing_a_new_incarnation : Specification
{
    static readonly EventStoreName _eventStore = "store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    InMemoryEncryptionKeyStorage _authority;
    ILocalSiloDetails _silo;
    EncryptionKeyFenceProtocol _publisher;
    string _oldId;
    string _newId;
    string? _oldRecord;
    string? _newRecord;

    void Establish()
    {
        _authority = new();
        _silo = Substitute.For<ILocalSiloDetails>();
        _silo.SiloAddress.Returns(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns([_eventStore]);
        storage.GetEventStore(_eventStore).Namespaces.GetAll().Returns([new NamespaceState(_namespace, DateTimeOffset.UnixEpoch)]);
        _publisher = new(storage, _authority, _authority, _silo, Options.Create(new ClusterOptions { ClusterId = "cluster" }), Options.Create(new ClusterMembershipOptions()), Options.Create(new ChronicleOptions()), NullLogger<EncryptionKeyFenceProtocol>.Instance, TimeProvider.System);
    }

    async Task Because()
    {
        _oldId = Id();
        await _publisher.Publish();
        _silo.SiloAddress.Returns(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 2));
        _newId = Id();
        await _publisher.Publish();
        _oldRecord = await _authority.ReadRecord(EventStoreName.System, EventStoreNamespaceName.Default, _oldId);
        _newRecord = await _authority.ReadRecord(EventStoreName.System, EventStoreNamespaceName.Default, _newId);
    }

    [Fact] void should_not_reuse_the_previous_incarnation_record() => _newId.ShouldNotEqual(_oldId);
    [Fact] void should_preserve_the_previous_record() => _oldRecord.ShouldNotBeNull();
    [Fact] void should_publish_the_current_protocol() => JsonDocument.Parse(_newRecord!).RootElement.GetProperty("Protocol").GetInt32().ShouldEqual(1);
    [Fact] void should_not_claim_a_floor_attestation() => _newRecord!.Contains("ProtocolFloor", StringComparison.Ordinal).ShouldBeFalse();

    string Id() => "protocol/" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { "cluster", _silo.SiloAddress.ToParsableString() })));

    void Destroy() => _publisher.Dispose();
}
