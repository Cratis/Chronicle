// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedSubjectValueHandler;

public class when_applying_reporting_the_key : Specification
{
    EncryptedSubjectValueHandler _handler;
    IManagedEncryptionKeyProvisioner _provisioner;
    Encryption _encryption;
    EncryptionKey _key;
    (JsonNode Value, EncryptionKeyUse? Key) _result;

    void Establish()
    {
        _encryption = new();
        _key = _encryption.GenerateKey();
        _provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        _provisioner.EnsureKeyWithRevisionFor("store", "tenant", EncryptedValueKeyIdentifiers.ForSubject("subject"))
            .Returns(new EncryptionKeyAtRevision(_key, new EncryptionKeyRevision(7u)));
        _handler = new(_provisioner, Substitute.For<IEncryptionKeyStorage>(), _encryption);
    }

    async Task Because() => _result = await _handler.ApplyReportingKey("store", "tenant", "subject", JsonValue.Create("secret"));

    [Fact] void should_report_the_key_used() => _result.Key!.Key.ShouldEqual(_key);
    [Fact] void should_report_the_revision_used() => _result.Key!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] void should_report_the_kind() => _result.Key!.Kind.ShouldEqual(ProtectionKind.EncryptedSubject);
    [Fact] void should_report_the_key_identifier() => _result.Key!.Identifier.ShouldEqual(EncryptedValueKeyIdentifiers.ForSubject("subject"));
    [Fact] void should_report_the_key_reference() => _result.Key!.KeyReference.ShouldEqual(_key.Fingerprint);
    [Fact] void should_decrypt_with_the_reported_key() => ProtectedValueCodec.Decrypt(_encryption, _result.Key!.Key, Convert.FromBase64String(_result.Value.ToString())).ToString().ShouldEqual("secret");
}
