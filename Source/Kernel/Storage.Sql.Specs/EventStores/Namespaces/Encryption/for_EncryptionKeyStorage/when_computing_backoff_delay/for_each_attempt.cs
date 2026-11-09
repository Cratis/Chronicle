// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.when_computing_backoff_delay;

public class for_each_attempt : Specification
{
    sealed class FixedRandom(bool maximum) : Random
    {
        public override int Next(int minValue, int maxValue) => maximum ? maxValue - 1 : minValue;
    }

    List<TimeSpan> _minimum = [];
    List<TimeSpan> _maximum = [];

    void Because()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            _minimum.Add(EncryptionKeyStorage.BackoffDelay(attempt, new FixedRandom(false)));
            _maximum.Add(EncryptionKeyStorage.BackoffDelay(attempt, new FixedRandom(true)));
        }
    }

    [Fact] void should_start_at_the_exponential_base() => _minimum.Take(6).Select(_ => (int)_.TotalMilliseconds).ShouldEqual([1, 2, 4, 8, 16, 32]);
    [Fact] void should_cap_the_base_at_thirty_two_milliseconds() => _minimum.Skip(5).ShouldEachConformTo(_ => _ == TimeSpan.FromMilliseconds(32));
    [Fact] void should_keep_jitter_below_twice_the_base() => _maximum.Take(6).Select(_ => (int)_.TotalMilliseconds).ShouldEqual([1, 3, 7, 15, 31, 63]);
    [Fact] void should_never_exceed_sixty_three_milliseconds() => _maximum.Max().ShouldEqual(TimeSpan.FromMilliseconds(63));
}
