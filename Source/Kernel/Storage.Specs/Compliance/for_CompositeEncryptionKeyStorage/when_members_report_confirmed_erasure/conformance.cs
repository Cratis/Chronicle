// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_members_report_confirmed_erasure;

public abstract class conformance : given.two_key_stores
{
    EncryptionKeyErasure _fence;
    EncryptionKeyErasureRequest _read;

    protected abstract EncryptionKeyFenceGuarantees MemberGuarantees { get; }

    protected abstract bool ExpectedConfirmation { get; }

    void Establish()
    {
        var primary = Substitute.For<IEncryptionKeyStorage>();
        var secondary = Substitute.For<IEncryptionKeyStorage>();
        primary.FenceGuarantees.Returns(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
        secondary.FenceGuarantees.Returns(MemberGuarantees);
        var request = new EncryptionKeyErasureRequest("request", 7u, 1, ["fingerprint"], Confirmed: true);
        foreach (var member in new[] { primary, secondary })
        {
            member.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
                .Returns(new EncryptionKeyErasure(7u, ["fingerprint"], false, Requests: [request]));
            member.GetErasureRequestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, request.RequestId).Returns(request);
        }
        _composite = new(primary, secondary);
    }

    async Task Because()
    {
        _fence = await _composite.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _read = await _composite.GetErasureRequestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, "request");
    }

    [Fact] public void should_only_confirm_the_fence_request_with_full_member_guarantees() => _fence.Requests.Single().Confirmed.ShouldEqual(ExpectedConfirmation);
    [Fact] public void should_only_confirm_the_read_request_with_full_member_guarantees() => _read.Confirmed.ShouldEqual(ExpectedConfirmation);
    [Fact] public void should_preserve_the_fence_boundary() => _fence.ErasedThrough.Value.ShouldEqual(7u);
    [Fact] public void should_preserve_the_read_boundary() => _read.Through.Value.ShouldEqual(7u);
}
