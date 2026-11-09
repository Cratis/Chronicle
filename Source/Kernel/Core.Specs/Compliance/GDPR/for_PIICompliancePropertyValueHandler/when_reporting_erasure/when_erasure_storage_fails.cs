// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_reporting_erasure;

public class when_erasure_storage_fails : given.a_property_handler
{
    Exception _error;

    void Establish()
    {
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKeyAtRevision?>(null));
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).ThrowsAsync(new EncryptionKeyStorageUnavailable(Identifier, []));
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseReportingErasure(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create(Convert.ToBase64String(new byte[256]))));

    [Fact] void should_not_claim_erasure() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
}
