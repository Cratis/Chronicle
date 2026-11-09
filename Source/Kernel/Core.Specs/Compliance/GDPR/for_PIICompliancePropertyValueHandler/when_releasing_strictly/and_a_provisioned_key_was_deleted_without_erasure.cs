// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_a_provisioned_key_was_deleted_without_erasure : Specification
{
    PIICompliancePropertyValueHandler _handler;
    JsonNode _value;
    Exception? _error;

    async Task Establish()
    {
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        _handler = new(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var key = encryption.GenerateKey();
        _value = ProtectedValueCodec.Encrypt(encryption, key, JsonValue.Create("protected value"));
        await keys.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", key);
        await keys.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner");
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", _value));

    [Fact] void should_report_the_missing_key_instead_of_returning_empty_data() => _error.ShouldBeOfExactType<MissingEncryptionKey>();
    [Fact]
    async Task should_fail_status_release_instead_of_reporting_erasure() =>
        await Assert.ThrowsAsync<MissingEncryptionKey>(() => _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", _value));
    [Fact]
    async Task should_not_report_erasure() =>
        (await _handler.ReleaseReportingErasure(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", _value)).IsErased.ShouldBeFalse();
}
