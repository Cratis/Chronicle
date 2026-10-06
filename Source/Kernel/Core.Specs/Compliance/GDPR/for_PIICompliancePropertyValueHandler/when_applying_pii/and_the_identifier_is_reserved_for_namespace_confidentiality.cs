// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_applying_pii;

public class and_the_identifier_is_reserved_for_namespace_confidentiality : given.a_property_handler
{
    Exception _exception;

    void Establish()
    {
        _provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        _provisioner.EnsureKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EncryptedValueKeyIdentifiers.ForNamespace()).Returns(_key);
        _encryption.Encrypt(Arg.Any<byte[]>(), _key).Returns([1, 2, 3]);
        _handler = new(_provisioner, _keyStore, _encryption);
    }

    async Task Because() => _exception = await Catch.Exception(() => _handler.Apply(
        EventStoreName.NotSet,
        EventStoreNamespaceName.NotSet,
        EncryptedValueKeyIdentifiers.ForNamespace().Value,
        JsonValue.Create("personal value")));

    [Fact] void should_reject_the_reserved_pii_identifier() => _exception.ShouldBeOfExactType<PIIIdentifierIsReserved>();
    [Fact] void should_not_encrypt_the_value() => _encryption.DidNotReceive().Encrypt(Arg.Any<byte[]>(), _key);
    [Fact] async Task should_not_provision_or_share_a_confidentiality_key() => await _provisioner.DidNotReceive().EnsureKeyFor(
        Arg.Any<EventStoreName>(),
        Arg.Any<EventStoreNamespaceName>(),
        Arg.Any<EncryptionKeyIdentifier>());
}
