// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.for_PublicClientApis.when_getting_empty_collections.and_jobs_have_no_entries.context;

namespace Cratis.Chronicle.Integration.for_PublicClientApis.when_getting_empty_collections;

[Collection(ChronicleCollection.Name)]
public class and_jobs_have_no_entries(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public IEnumerable<Job> Result;

        async Task Because() => Result = await EventStore.Jobs.GetJobs();
    }

    [Fact] void should_return_an_empty_collection() => Context.Result.ShouldBeEmpty();
}
