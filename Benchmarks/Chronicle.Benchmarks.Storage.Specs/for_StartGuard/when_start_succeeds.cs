// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_StartGuard;

public class when_start_succeeds : Specification
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
    Host _result = default!;

    void Establish() => _host = new();

    async Task Because() => _result = await StartGuard.StartOrDispose(_host, _ => Task.CompletedTask);

    [Fact] void should_return_the_host() => _result.ShouldEqual(_host);
    [Fact] void should_not_dispose_it() => _host.Disposed.ShouldBeFalse();
}
