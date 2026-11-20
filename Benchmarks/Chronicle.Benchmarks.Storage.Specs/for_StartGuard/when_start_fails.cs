// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_StartGuard;

public class when_start_fails : Specification
{
    sealed class Host : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    Host _host = default!;
    Exception? _error;

    void Establish() => _host = new();

    async Task Because() => _error = await Catch.Exception(() => StartGuard.StartOrDispose(_host, _ => throw new InvalidOperationException("boom")));

    [Fact] void should_dispose_the_partial_host() => _host.Disposed.ShouldBeTrue();
    [Fact] void should_rethrow() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
