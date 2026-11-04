// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.given;
using Cratis.Chronicle.Storage.Changes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_using_compatible_step_constructors;

public class and_a_pinned_future_is_released : a_first_level_child_future
{
    bool _queuedBeforeRoot;

    void Establish()
    {
        SetFutureKey("root-key");
        _projection.GenerationDelivery.Returns(EventGenerationDelivery.Pinned);
        _projectionFutures.AddPinnedFuture(_future).Returns(1);
        _tracker.HasPending = false;
        _context.AddDeferredFuture(_future);
        _step = new ResolveFutures(_projectionFutures, _tracker, _typeFormats, new ObjectComparer(), _futureProtection, _logger, _eventSequenceStorage, _sink);
    }

    async Task Because()
    {
        var store = new StoreFutures(_projectionFutures, _tracker, NullLogger<StoreFutures>.Instance);
        await store.Perform(_projection, _context);
        var beforeRoot = await _step.Perform(_projection, _context);
        _queuedBeforeRoot = beforeRoot.PendingFutureSaves.Any();
        await ProcessRoot(RootWith(initialized: true));
        var save = new SaveChanges(_sink, Substitute.For<IChangesetStorage>(), true, NullLogger<SaveChanges>.Instance, _projectionFutures);
        await save.Perform(_projection, _result!);
    }

    [Fact] async Task should_store_the_pinned_generation() => await _projectionFutures.Received(1).AddPinnedFuture(_future);
    [Fact] async Task should_not_use_compatibility_deduplication() => await _projectionFutures.DidNotReceive().AddFuture(Arg.Any<ProjectionFuture>());
    [Fact] void should_not_release_before_the_root_exists() => _queuedBeforeRoot.ShouldBeFalse();
    [Fact] void should_project_the_child_when_the_root_arrives() => HasChild.ShouldBeTrue();
    [Fact] async Task should_acknowledge_the_shared_future_after_saving() => await _projectionFutures.Received(1).ResolveFuture(_future.Id);
}
