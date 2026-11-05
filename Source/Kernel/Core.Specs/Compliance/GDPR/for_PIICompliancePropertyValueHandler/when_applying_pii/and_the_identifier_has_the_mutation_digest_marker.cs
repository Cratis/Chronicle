// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_applying_pii;

public class and_the_identifier_has_the_mutation_digest_marker : given.a_property_handler
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
        EventSequenceMutationDigestKeyIdentifiers.Secret.Value,
        JsonValue.Create("personal value")));

    [Fact] void should_refuse_the_reserved_identifier() => _exception.ShouldBeOfExactType<PIIIdentifierIsReserved>();
    [Fact] async Task should_not_provision_a_key() => await _provisioner.DidNotReceive().EnsureKeyWithRevisionFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>());
}
