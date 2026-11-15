// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;
using ContractRequest = Cratis.Chronicle.Contracts.Compliance.DeleteEncryptionKeyRequest;

namespace Cratis.Chronicle.Compliance.for_DeleteEncryptionKeyRequest;

public class when_reading_old_and_new_clients
{
    [Fact]
    public void should_default_the_core_identity_to_absent() => new DeleteEncryptionKeyRequest("store", "namespace", "subject").RequestId.ShouldBeNull();

    [Fact]
    public void should_read_an_old_client_without_inventing_a_retry_identity()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new given.LegacyErasureRequest { EventStore = "store", Namespace = "namespace", Identifier = "subject" });
        stream.Position = 0;
        var request = Serializer.Deserialize<ContractRequest>(stream);
        request.EventStore.ShouldEqual("store");
        request.Namespace.ShouldEqual("namespace");
        request.Identifier.ShouldEqual("subject");
        request.RequestId.ShouldBeNull();
    }

    [Fact]
    public void should_keep_the_original_fields_readable_to_an_old_client()
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, new ContractRequest { EventStore = "store", Namespace = "namespace", Identifier = "subject", RequestId = " opaque ? " });
        stream.Position = 0;
        var old = Serializer.Deserialize<given.LegacyErasureRequest>(stream);
        old.EventStore.ShouldEqual("store");
        old.Namespace.ShouldEqual("namespace");
        old.Identifier.ShouldEqual("subject");
    }
}
