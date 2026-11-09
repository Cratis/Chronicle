// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyFenceUpdates;

public class when_an_old_writer_overwrites_a_backfilled_fence : Specification
{
    EncryptionKeyErasure _backfilled;
    EncryptionKeyErasure _overwritten;

    void Because()
    {
        var legacy = new EncryptionKeyErasure(3u, [], false);
        _backfilled = legacy with { Seal = EncryptionKeyFenceUpdates.Seal(legacy) };

        // A pre-N replacement drops both Version and Seal back to their legacy defaults.
        _overwritten = new(3u, [], false);
    }

    [Fact] void should_accept_the_backfilled_fence_in_n_plus_one() => EncryptionKeyStorage.HasValidSeal(_backfilled, requireSeal: true).ShouldBeTrue();
    [Fact] void should_refuse_a_missing_seal_even_when_version_is_zero_in_n_plus_one() => EncryptionKeyStorage.HasValidSeal(_overwritten, requireSeal: true).ShouldBeFalse();
    [Fact] void should_preserve_release_n_legacy_reads() => EncryptionKeyStorage.HasValidSeal(_overwritten).ShouldBeTrue();
}
