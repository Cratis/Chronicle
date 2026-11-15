// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_erasure_is_confirmed_while_a_release_is_paused : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    readonly InMemoryEncryptionKeyStorage _storage = new();
    readonly TaskCompletionSource _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _handedOff;
    Exception _error;
    EncryptionKeyErasureRequest _request;

    async Task Because()
    {
        await _storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _pair.Key);
        var release = PausedRelease();
        await _paused.Task;
        await _storage.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new EncryptionKeyErasureRequest("erase"));
        await _storage.DeleteThroughFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, "erase");
        _clock.Advance(TimeSpan.FromSeconds(6));
        await _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, "erase");
        _request = await _storage.GetErasureRequestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, "erase");
        _resume.SetResult();
        _error = await Catch.Exception(() => release);
    }

    async Task PausedRelease()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        await Release(_storage);
        _paused.SetResult();
        await _resume.Task;
        await ReleaseKeys.Complete(() => _handedOff = true);
    }

    [Fact] void should_have_confirmed_the_erasure() => _request.Confirmed.ShouldBeTrue();
    [Fact] void should_refuse_the_paused_release() => _error.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_not_hand_off_material_after_confirmation() => _handedOff.ShouldBeFalse();
}
