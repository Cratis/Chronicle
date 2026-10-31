// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class when_locating_the_key : Specification
{
    PIICompliancePropertyValueHandler _handler;
    IEncryptionKeyStorage _storage;
    IManagedEncryptionKeyProvisioner _provisioner;
    ProtectionKeyLocation _result;

    void Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        _handler = new(_provisioner, _storage, Substitute.For<IEncryption>());
    }

    void Because() => _result = _handler.KeyLocationFor("subject");

    [Fact] void should_name_the_kind() => _result.Kind.ShouldEqual(ProtectionKind.Pii);
    [Fact] void should_name_the_identifier() => _result.Identifier.ShouldEqual((EncryptionKeyIdentifier)"subject");
    [Fact] void should_not_read_keys() => _storage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_provision_keys() => _provisioner.ReceivedCalls().ShouldBeEmpty();
}
