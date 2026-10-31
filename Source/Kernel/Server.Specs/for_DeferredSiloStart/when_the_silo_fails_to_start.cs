// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_DeferredSiloStart;

public class when_the_silo_fails_to_start : Specification
{
    IHostedService _silo;
    InvalidOperationException _failure;
    Exception? _error;

    void Establish()
    {
        _failure = new("Silo startup failed.");
        _silo = Substitute.For<IHostedService>();
        _silo.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(_failure));
    }

    async Task Because() => _error = await Catch.Exception(() => new DeferredSiloStart(_silo, NullLogger<Kernel>.Instance, []).StartedAsync(CancellationToken.None));

    [Fact] void should_propagate_the_failure_to_the_host() => _error.ShouldBeSame(_failure);
}
