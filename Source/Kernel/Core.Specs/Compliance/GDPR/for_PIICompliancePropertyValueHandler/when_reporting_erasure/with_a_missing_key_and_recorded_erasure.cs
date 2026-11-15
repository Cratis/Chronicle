// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_reporting_erasure;

public class with_a_missing_key_and_recorded_erasure : given.a_property_handler
{
    (JsonNode Value, bool IsErased) _result;

    void Establish()
    {
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKeyAtRevision?>(null));
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(1, [], true));
    }

    async Task Because() => _result = await _handler.ReleaseReportingErasure(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create(Convert.ToBase64String(new byte[256])));

    [Fact] void should_report_erasure() => _result.IsErased.ShouldBeTrue();
    [Fact] void should_keep_the_existing_empty_value() => _result.Value.ToString().ShouldEqual(string.Empty);
    [Fact] void should_not_provision_a_key() => _encryption.DidNotReceive().GenerateKey();
}
