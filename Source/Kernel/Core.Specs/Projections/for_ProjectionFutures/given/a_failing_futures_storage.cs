// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionFutures.given;

public class a_failing_futures_storage : the_futures_grain
{
    protected IStorage<ProjectionFuturesState> _storage;
    protected ProjectionFuture _future;
    protected readonly Exception _writeFailure = new FuturesStorageUnavailable();
    protected Exception? _error;

    async Task Establish()
    {
        _future = CreateFuture(1);
        _storage = Substitute.For<IStorage<ProjectionFuturesState>>();
        _storage.State = new ProjectionFuturesState();
        _storage.WriteStateAsync().Returns(Task.FromException(_writeFailure));
        var silo = new TestKitSilo();
        silo.Options.StorageFactory = _ => _storage;
        _grain = await silo.CreateGrainAsync<ProjectionFutures>("test-projection");
    }

    sealed class FuturesStorageUnavailable() : Exception("Futures storage is unavailable");
}
