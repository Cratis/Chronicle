// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueOperations.given;

public class a_signed_confidentiality_value : Compliance.for_Encryption.given.a_signed_value
{
    protected Exception _strictFailure;
    protected Exception _statusFailure;
    protected JsonNode _strictValue;
    protected JsonNode _lenientValue;
    protected ReleasedProtectedValue _statusValue;
    IEncryptionKeyStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _storage.TryGetAtRevisionFor(_address.EventStore, _address.Namespace, Arg.Any<EncryptionKeyIdentifier>(), new EncryptionKeyRevision(7))
            .Returns(new EncryptionKeyAtRevision(_key, 7));
    }

    protected async Task ReleaseAt(ProtectedValueAddress address)
    {
        var value = JsonValue.Create(Convert.ToBase64String(_encrypted));
        _strictFailure = await Catch.Exception(async () => _strictValue = await EncryptedValueOperations.Release(
            _storage, _encryption, address.EventStore, address.Namespace, address.Identifier, value, strict: true));
        _statusFailure = await Catch.Exception(async () => _statusValue = await EncryptedValueOperations.ReleaseWithStatus(
            _storage, _encryption, address.EventStore, address.Namespace, address.Identifier, value));
        _lenientValue = await EncryptedValueOperations.Release(
            _storage, _encryption, address.EventStore, address.Namespace, address.Identifier, value);
    }
}
