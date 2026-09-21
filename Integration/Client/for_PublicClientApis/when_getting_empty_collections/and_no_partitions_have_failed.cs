// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using context = Cratis.Chronicle.Integration.for_PublicClientApis.when_getting_empty_collections.and_no_partitions_have_failed.context;

namespace Cratis.Chronicle.Integration.for_PublicClientApis.when_getting_empty_collections;

[Collection(ChronicleCollection.Name)]
public class and_no_partitions_have_failed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public IEnumerable<FailedPartition> Result;

        async Task Because() => Result = await EventStore.FailedPartitions.GetAllFailedPartitions();
    }

    [Fact] void should_return_an_empty_collection() => Context.Result.ShouldBeEmpty();
}
