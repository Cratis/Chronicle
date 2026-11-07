// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures;

public class when_protecting_a_future_fails : given.a_first_level_child_future
{
    void Establish()
    {
        SetFutureKey("root-key");
        _futureProtection.When(_ => _.ProtectFutureChangeset(Arg.Any<IProjection>(), Arg.Any<ProjectionEventContext>()).AsTask())
            .Do(_ => throw new InvalidOperationException("Protection failed"));
    }

    async Task Because() => await ProcessRoot(RootWith("id", "root-key"));

    [Fact] void should_not_queue_the_future_save() => _result!.PendingFutureSaves.ShouldBeEmpty();
    [Fact] void should_not_resolve_the_future() => _projectionFutures.DidNotReceive().ResolveFuture(_future.Id);
}
