// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.Storage.Sinks;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionCatchupHandler.when_catching_up_a_generation;

public class and_admission_fails_at_end : given.a_generation_bound_catchup_handler
{
    GenerationSinkContext _reissued;

    async Task Establish()
    {
        await _handler.BeginCatchupFor(_details);
        _generations.Admit(_details.Key.EventStore, _execution).ThrowsAsync(new GenerationWriterGrantNotAdmitted());
        await CreateHandler().EndCatchupFor(_details);
        _reissued = GenerationExecutions.Context(_execution, 2);
        _generations.Admit(_details.Key.EventStore, _execution).Returns(_reissued);
    }

    async Task Because() => await CreateHandler().ResumeCatchupFor(_details);

    [Fact] void should_forget_the_failed_window() => _generationPipelines.Received(1).GetFor(_details.Key.EventStore, _details.Key.Namespace, _generationProjection, _reissued);
    [Fact] void should_resolve_a_new_window() => _generations.Received(2).Resolve(_details.Key.EventStore, _details.Key.Namespace, _details.Key.EventSequenceId, _details.Key.ObserverId);
}
