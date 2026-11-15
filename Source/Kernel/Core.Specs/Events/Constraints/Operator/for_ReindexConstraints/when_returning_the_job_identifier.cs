// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_returning_the_job_identifier : given.a_reindex_command
{
    JobId _returned;

    async Task Because() => _returned = await new ReindexConstraints("store", "ns", "log", []).Handle(_grainFactory, _indexes);

    [Fact] void should_return_the_started_job_identifier() => _returned.ShouldEqual(_jobId);
}
