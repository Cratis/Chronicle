// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

/// <summary>
/// Erasing a subject removes the key that exists now; it does not ban the identifier forever. This is the call a
/// consumer makes when the same person has a lawful basis to be protected again.
/// </summary>
public class when_allowing_a_new_encryption_key_for_a_subject : given.a_pii_manager
{
    static readonly Subject _subject = "person-42";

    async Task Because() => await _manager.AllowNewEncryptionKeyFor(_subject);

    [Fact]
    async Task should_allow_a_new_key_for_the_store() =>
        await _compliance.Received(1).AllowNewEncryptionKey(
            Arg.Is<AllowNewEncryptionKeyRequest>(_ => _.EventStore == _eventStore.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_allow_a_new_key_for_the_namespace() =>
        await _compliance.Received(1).AllowNewEncryptionKey(
            Arg.Is<AllowNewEncryptionKeyRequest>(_ => _.Namespace == _namespace.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_allow_a_new_key_for_the_subject_identifier() =>
        await _compliance.Received(1).AllowNewEncryptionKey(
            Arg.Is<AllowNewEncryptionKeyRequest>(_ => _.Identifier == _subject.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_not_erase_anything() =>
        await _compliance.DidNotReceive().DeleteEncryptionKey(Arg.Any<DeleteEncryptionKeyRequest>(), Arg.Any<CallContext>());
}
