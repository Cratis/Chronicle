// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_forgetting_resolved_retry_counts;

public class and_no_retry_counts_are_remembered : Specification
{
    Dictionary<FailedPartitionId, int> _counts;
    counting_failed_partitions _unresolved;

    void Establish()
    {
        _counts = [];
        _unresolved = new();
    }

    void Because() => Observer.ForgetResolvedRetryCounts(_counts, _unresolved);

    [Fact] void should_remain_empty() => _counts.ShouldBeEmpty();
    [Fact] void should_not_enumerate_the_unresolved_episodes() => _unresolved.Enumerations.ShouldEqual(0);

    class counting_failed_partitions : IEnumerable<FailedPartition>
    {
        public int Enumerations { get; private set; }

        public IEnumerator<FailedPartition> GetEnumerator()
        {
            Enumerations++;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
