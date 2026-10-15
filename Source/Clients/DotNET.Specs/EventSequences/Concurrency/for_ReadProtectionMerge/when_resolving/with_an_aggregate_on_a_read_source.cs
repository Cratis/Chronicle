// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_an_aggregate_on_a_read_source : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Aggregate, new(90, _label), "stock aggregate");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_the_aggregate_read_combination() => _error.ShouldBeOfExactType<Transactions.ReadAndAggregateOnSameSource>();
}
