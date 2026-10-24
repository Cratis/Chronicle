// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork;

public class when_job_types_cannot_be_classified : given.legacy_work
{
    Exception _error;

    void Establish() => _namespace.Jobs.GetJobs<IReplayObserver, JobState>().Returns(Task.FromResult<Cratis.Monads.Catch<IImmutableList<JobState>, JobError>>(JobError.TypeIsNotAssociatedWithAJobType));

    async Task Because() => _error = await Catch.Exception(() => _reader.Inspect(new("orders")));

    [Fact] void should_refuse_to_infer_drained_work() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
