// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

public class when_one_member_has_no_fencing_guarantees : given.two_key_stores
{
    Exception? _error;
    EncryptionKeyErasure? _fence;

    async Task Establish()
    {
        var legacy = Substitute.For<IEncryptionKeyStorage>();
        legacy.Capabilities.Returns([new("legacy", EncryptionKeyFenceGuarantees.None)]);
        _composite = new(_primary, legacy, _secondary);
        await Save(_primary, KeyNamed("original"));
        await Save(_secondary, KeyNamed("original"));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _composite.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new("request")));
        _fence = await _secondary.GetFenceFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_not_advertise_a_stronger_composite() => _composite.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
    [Fact] void should_not_claim_the_erasure_completed() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_still_fence_members_after_the_legacy_member() => _fence.Requests.Single().RequestId.ShouldEqual("request");
    [Fact] void should_expose_each_member_capability() => _composite.Capabilities.Count.ShouldEqual(3);
}
