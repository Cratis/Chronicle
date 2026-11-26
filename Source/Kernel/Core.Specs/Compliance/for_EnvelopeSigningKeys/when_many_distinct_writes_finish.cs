// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EnvelopeSigningKeys;

public class when_many_distinct_writes_finish : given.a_signing_key_cache
{
    bool _bounded;
    bool _released;
    int _maximumActive;

    void Establish() => _cache = new(capacity: 4);

    void Because()
    {
        _bounded = true;
        _released = true;
        for (var index = 0; index < 256; index++)
        {
            var commitment = BitConverter.GetBytes(index);
            using (var operation = _cache.Begin(_key, 7, commitment))
            {
                operation.Acquire();
                _maximumActive = Math.Max(_maximumActive, _cache.ActiveCount);
                if (index % 2 == 0) _cache.Evict(commitment, new EncryptionKeyErasure(7, [_key.Fingerprint], false));
                _bounded &= _cache.Count <= 4;
            }

            _released &= _cache.ActiveCount == 0;
        }
    }

    [Fact] void should_bound_the_cached_entries() => _bounded.ShouldBeTrue();
    [Fact] void should_track_only_the_in_flight_write() => _maximumActive.ShouldEqual(1);
    [Fact] void should_release_bookkeeping_after_every_live_or_erased_write() => _released.ShouldBeTrue();
    [Fact] void should_retain_no_finished_write_identities() => _cache.ActiveCount.ShouldEqual(0);
}
