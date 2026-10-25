// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_replaying_a_generation;

public class and_admission_fails_at_end : given.a_generation_bound_replay_handler
{
    GenerationSinkContext _reissued;

    async Task Establish()
    {
        await _handler.BeginReplayFor(_observerDetails);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
        await CreateHandler().EndReplayFor(_observerDetails);
        _reissued = GenerationExecutions.Context(_execution, 2);
        _generations.Admit(_observerDetails.Key.EventStore, _execution).Returns(_reissued);
    }

    async Task Because() => await CreateHandler().ResumeReplayFor(_observerDetails);

    [Fact] void should_forget_the_failed_window() => _generationPipelines.Received(1).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _generationProjection, _reissued);
    [Fact] void should_resolve_a_new_window() => _generations.Received(2).Resolve(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.EventSequenceId, _observerDetails.Key.ObserverId);
}
