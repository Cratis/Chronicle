// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

public class when_provisioning_with_a_non_fencing_member : given.two_key_stores
{
    EncryptionKeyAtRevision _provisioned;
    EncryptionKeyAtRevision? _read;
    EncryptionKeyErasureRequest? _request;
    Exception _confirmationError;

    void Establish()
    {
        var legacy = Substitute.For<IEncryptionKeyStorage>();
        legacy.GetErasureRequestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, "request")
            .Returns(new EncryptionKeyErasureRequest("request", Confirmed: true));
        _composite = new(_primary, legacy);
    }

    async Task Because()
    {
        _provisioned = await _composite.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("original"));
        _read = await _composite.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await Catch.Exception(() => _composite.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new("request")));
        await _primary.DeleteThroughFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, "request");
        _confirmationError = await Catch.Exception(() => _composite.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, "request"));
        _request = await _composite.GetErasureRequestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, "request");
    }

    [Fact] void should_provision_in_release_n() => _provisioned.Key.Public.ShouldEqual(KeyNamed("original").Public);
    [Fact] void should_read_in_release_n() => _read.Key.Public.ShouldEqual(_provisioned.Key.Public);
    [Fact] void should_refuse_protocol_confirmation() => _confirmationError.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_leave_the_request_pending_even_if_members_claim_confirmation() => _request.Confirmed.ShouldBeFalse();
}
