// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_forgetting_resolved_retry_counts;

public class and_some_episodes_are_still_unresolved : Specification
{
    static readonly FailedPartitionId _a = new(new Guid("00000000-0000-0000-0000-000000000001"));
    static readonly FailedPartitionId _b = new(new Guid("00000000-0000-0000-0000-000000000002"));
    static readonly FailedPartitionId _c = new(new Guid("00000000-0000-0000-0000-000000000003"));
    static readonly FailedPartitionId _d = new(new Guid("00000000-0000-0000-0000-000000000004"));
    static readonly FailedPartitionId _e = new(new Guid("00000000-0000-0000-0000-000000000005"));

    Dictionary<FailedPartitionId, int> _counts;
    counting_failed_partitions _unresolved;

    void Establish()
    {
        _counts = new()
        {
            [_a] = 7,
            [_b] = 2,
            [_c] = 9,
            [_d] = 1
        };

        _unresolved = new(
            new FailedPartition { Id = _a, Partition = (Key)"a" },
            new FailedPartition { Id = new FailedPartitionId(_b.Value), Partition = (Key)"b" },
            new FailedPartition { Id = _e, Partition = (Key)"c" });
    }

    void Because() => Observer.ForgetResolvedRetryCounts(_counts, _unresolved);

    [Fact] void should_keep_only_the_unresolved_episodes() => _counts.Keys.ShouldContainOnly(_a, _b);
    [Fact] void should_keep_their_counts() => (_counts[_a], _counts[_b]).ShouldEqual((7, 2));
    [Fact] void should_enumerate_the_unresolved_episodes_once() => _unresolved.Enumerations.ShouldEqual(1);
    [Fact] void should_yield_each_unresolved_episode_once() => _unresolved.Yielded.ShouldEqual(3);

    class counting_failed_partitions(params FailedPartition[] partitions) : IEnumerable<FailedPartition>
    {
        public int Enumerations { get; private set; }

        public int Yielded { get; private set; }

        public IEnumerator<FailedPartition> GetEnumerator()
        {
            Enumerations++;
            foreach (var partition in partitions)
            {
                Yielded++;
                yield return partition;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
