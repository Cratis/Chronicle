// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintDefinitionExtensions.given;

public class a_pii_value_encrypted_per_subject : Specification
{
    protected const string Property = "Email";
    protected const string Value = "jane@example.com";
    protected const string FirstSubject = "b8f1d3e0-0000-0000-0000-000000000001";
    protected const string SecondSubject = "b8f1d3e0-0000-0000-0000-000000000002";

    protected PIICompliancePropertyValueHandler _handler;
    protected Encryption _encryption;
    protected IEncryptionKeyStorage _keyStore;

    void Establish()
    {
        _encryption = new();
        _keyStore = Substitute.For<IEncryptionKeyStorage>();

        // Every subject has its own encryption key, so the same plaintext encrypts to different ciphertext.
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, FirstSubject).Returns(Task.FromResult<EncryptionKey?>(_encryption.GenerateKey()));
        _keyStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, SecondSubject).Returns(Task.FromResult<EncryptionKey?>(_encryption.GenerateKey()));

        _handler = new(_keyStore, _encryption);
    }

    protected static string HashOf(string value) =>
        new List<UniqueConstraintPropertyAndValue> { new(Property, value) }.GetValue();
}
