// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_the_selected_historical_key_is_erased_before_completion : given.a_rotated_key
{
    async Task Because()
    {
        var clock = new for_ReleaseKeys.given.a_release_clock();
        using var scope = ReleaseKeys.BeginScope(clock);
        var plaintext = await new ProtectedValueRelease(_storage, _encryption, Address, _encrypted).Release();
        _fence = new EncryptionKeyErasure(2, [], true);
        clock.Advance(TimeSpan.FromSeconds(6));
        _failure = await Catch.Exception(async () => _released = await ReleaseKeys.Complete(() => plaintext));
    }

    [Fact] void should_revalidate_the_selected_historical_key() => _failure.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_release_plaintext_past_the_fence() => _released.ShouldBeNull();
}
