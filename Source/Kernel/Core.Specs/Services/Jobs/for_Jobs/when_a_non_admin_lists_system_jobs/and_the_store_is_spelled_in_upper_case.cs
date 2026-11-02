// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.Jobs.for_Jobs.when_a_non_admin_lists_system_jobs;

public class and_the_store_is_spelled_in_upper_case : for_QueryExecutor.given.a_routed_query
{
    internal Jobs _jobs;
    IGrainFactory _grainFactory;
    IStorage _storage;
    QueryResult<IEnumerable<JobSummaryResponse>> _result;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _jobs = new Jobs(Substitute.For<Cratis.Arc.Commands.ICommandPipeline>(), _storage, _grainFactory, new for_Jobs.given.RecordingLogger<Jobs>());
    }

    async Task Because() => _result = await _jobs.AllJobs(new AllJobsRequest { EventStore = "SYSTEM", Namespace = "Default" }, CallFor(new Probe(), CallerNamed(OrdinaryUser)));

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_touch_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
}
