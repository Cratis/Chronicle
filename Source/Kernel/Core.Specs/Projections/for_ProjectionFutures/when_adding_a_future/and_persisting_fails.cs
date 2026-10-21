// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionFutures.when_adding_a_future;

public class and_persisting_fails : given.a_failing_futures_storage
{
    async Task Because() => _error = await Catch.Exception(() => _grain.AddFuture(_future));

    [Fact] async Task should_not_list_the_future() => (await _grain.GetFutures()).ShouldBeEmpty();
    [Fact] void should_rethrow_the_failure() => _error.ShouldEqual(_writeFailure);
    [Fact] void should_clear_pending_additions() => _storage.State.AddedFutures.ShouldBeEmpty();
}
