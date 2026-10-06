// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_applying_pii;

public class and_the_identifier_has_the_reserved_marker : given.a_property_handler
{
    Exception _exception;

    void Establish()
    {
        _provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        _handler = new(_provisioner, _keyStore, _encryption);
    }

    async Task Because() => _exception = await Catch.Exception(() => _handler.Apply(
        EventStoreName.NotSet,
        EventStoreNamespaceName.NotSet,
        $"{EncryptedValueKeyIdentifiers.Marker}arbitrary-subject",
        JsonValue.Create("personal value")));

    [Fact] void should_reject_any_reserved_prefix_not_only_known_key_identifiers() => _exception.ShouldBeOfExactType<PIIIdentifierIsReserved>();
    [Fact] async Task should_not_provision_a_key() => await _provisioner.DidNotReceive().EnsureKeyFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>());
}
