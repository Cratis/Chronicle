// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

public class when_deleting_encryption_key_for_a_subject : given.a_pii_manager
{
    static readonly Subject _subject = "person-42";

    async Task Because() => await _manager.DeleteEncryptionKeyFor(_subject);

    [Fact]
    async Task should_delete_the_key_for_the_store() =>
        await _compliance.Received(1).DeleteEncryptionKey(
            Arg.Is<DeleteEncryptionKeyRequest>(_ => _.EventStore == _eventStore.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_delete_the_key_for_the_namespace() =>
        await _compliance.Received(1).DeleteEncryptionKey(
            Arg.Is<DeleteEncryptionKeyRequest>(_ => _.Namespace == _namespace.Value),
            Arg.Any<CallContext>());

    [Fact]
    async Task should_delete_the_key_for_the_subject_identifier() =>
        await _compliance.Received(1).DeleteEncryptionKey(
            Arg.Is<DeleteEncryptionKeyRequest>(_ => _.Identifier == _subject.Value),
            Arg.Any<CallContext>());
}
