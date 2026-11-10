// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_disposing;

/// <summary>
/// The container disposes the keeper once for every registration that exposes it.
/// </summary>
public class and_it_is_disposed_again : given.a_keeper
{
    Exception? _error;

    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        await Observer.OnStop(CancellationToken.None);
        Keeper.Dispose();
        _error = Catch.Exception(Keeper.Dispose);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
