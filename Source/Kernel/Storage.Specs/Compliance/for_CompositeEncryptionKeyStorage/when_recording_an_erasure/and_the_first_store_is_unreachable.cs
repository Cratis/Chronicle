// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_recording_an_erasure;

/// <summary>
/// The same discipline the deletion follows, applied to the fence: stopping at the first failure would leave a
/// store that was never even attempted free to provision the subject a key again, while the caller sees the same
/// exception either way.
/// </summary>
public class and_the_first_store_is_unreachable : given.two_key_stores
{
    Exception _error;
    EncryptionKeyErasure? _inSecondary;

    void Establish() => _composite = new CompositeEncryptionKeyStorage(AnUnreachableStore(), _secondary);

    async Task Because()
    {
        _error = await Catch.Exception(async () => await _composite.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier));
        _inSecondary = await _secondary.GetErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_still_fence_every_reachable_store() => _inSecondary.ShouldNotBeNull();
    [Fact] void should_report_the_erasure_as_incomplete() => _error.ShouldBeOfExactType<EncryptionKeyErasureIncomplete>();
    [Fact] void should_carry_the_failure_from_the_unreachable_store() => ((EncryptionKeyErasureIncomplete)_error).Failures.ShouldContainOnly(_error.InnerException);
}
