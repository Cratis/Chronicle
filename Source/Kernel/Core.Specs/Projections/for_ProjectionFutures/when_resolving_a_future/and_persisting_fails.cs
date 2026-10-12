// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionFutures.when_resolving_a_future;

public class and_persisting_fails : given.a_failing_futures_storage
{
    void Establish() => _storage.State.Futures.Add(_future);

    async Task Because() => _error = await Catch.Exception(() => _grain.ResolveFuture(_future.Id));

    [Fact] async Task should_keep_the_future_listed() => (await _grain.GetFutures()).ShouldContainOnly(_future);
    [Fact] void should_rethrow_the_failure() => _error.ShouldEqual(_writeFailure);
    [Fact] void should_clear_pending_removals() => _storage.State.ResolvedFutures.ShouldBeEmpty();
}
