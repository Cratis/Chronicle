// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.given;

public class an_append_intent : Specification
{
    internal AppendFingerprintInput _input;
    internal StoredAppendFingerprint _original;
    internal StoredAppendFingerprint _result;
    protected EncryptionKey _key;

    void Establish()
    {
        _key = new([1, 2, 3], [4, 5, 6]);
        _input = new(new EventType("event", 1), JsonNode.Parse("""{"name":"Alice","count":1}""")!.AsObject(), "source", EventSourceType.Default, EventStreamType.All, EventStreamId.Default, null, "source", "append-key:test", []);
        _original = Compute(_input);
    }

    internal static StoredAppendFingerprint Compute(AppendFingerprintInput input, params ProtectedValueUse[] uses)
    {
        AppendFingerprint.Compute(input, uses).TryGetResult(out var fingerprint).ShouldBeTrue();
        return fingerprint;
    }

    internal void Protect(string path = "name", ProtectionKind kind = ProtectionKind.Pii, EncryptionKeyRevision? revision = null)
    {
        _input = _input with { ProtectedValues = [new(path, kind, "subject", _input.Content[path]!.DeepClone())] };
        _original = Compute(_input, Use(path, kind, _key, revision));
    }

    internal ProtectedValueUse Use(string path, ProtectionKind kind, EncryptionKey key, EncryptionKeyRevision? revision = null) =>
        new(path, kind, _input.Content[path]!.DeepClone(), new(kind, "subject", key, revision ?? EncryptionKeyRevision.Initial));
}
