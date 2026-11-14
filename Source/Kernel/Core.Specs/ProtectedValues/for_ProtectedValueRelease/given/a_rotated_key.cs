// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease.given;

public class a_rotated_key : Specification
{
    protected static readonly (EventStoreName, EventStoreNamespaceName, EncryptionKeyIdentifier) Address = ("store", "namespace", "subject");
    protected Encryption _encryption;
    protected IEncryptionKeyStorage _storage;
    protected EncryptionKeyErasure _fence;
    protected EncryptionKeyAtRevision _old;
    protected EncryptionKeyAtRevision _latest;
    protected Dictionary<uint, EncryptionKeyAtRevision> _revisions;
    protected byte[] _encrypted;
    protected JsonNode _released;
    protected Exception _failure;

    void Establish()
    {
        _encryption = new();
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _storage.GetErasureForRelease(Address.Item1, Address.Item2, Address.Item3).Returns(_ => _fence);
        _old = new(_encryption.GenerateKey(), 2);
        _latest = new(_encryption.GenerateKey(), 4);
        _revisions = new() { [2] = _old };
        _storage.TryGetLatestFor(Address.Item1, Address.Item2, Address.Item3).Returns(_ => _latest);
        _storage.TryGetAtRevisionFor(Address.Item1, Address.Item2, Address.Item3, Arg.Any<EncryptionKeyRevision>())
            .Returns(call => _revisions.GetValueOrDefault(call.ArgAt<EncryptionKeyRevision>(3).Value));
        _encrypted = _encryption.Encrypt("\"value\""u8.ToArray(), _old.Key, _old.Revision);
    }

    protected async Task Release()
    {
        using var scope = ReleaseKeys.BeginScope();
        _released = await new ProtectedValueRelease(_storage, _encryption, Address, _encrypted).Release();
        _released = await ReleaseKeys.Complete(() => _released);
    }
}
