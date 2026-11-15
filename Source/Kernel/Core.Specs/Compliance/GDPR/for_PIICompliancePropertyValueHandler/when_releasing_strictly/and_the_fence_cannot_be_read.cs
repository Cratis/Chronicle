// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_the_fence_cannot_be_read : given.a_property_handler
{
    JsonNode _value;
    Exception _error;
    readonly Exception _failure = new InvalidOperationException();

    void Establish()
    {
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).ThrowsAsync(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_preserve_the_cause() => _error.InnerException.ShouldBeSame(_failure);
    [Fact] void should_not_decrypt() => _encryption.DidNotReceive().Decrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
