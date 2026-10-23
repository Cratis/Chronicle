// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_DeferredSiloStart;

public class when_starting : Specification
{
    IHostedService _silo;
    CancellationTokenSource _token;

    void Establish()
    {
        _silo = Substitute.For<IHostedService>();
        _token = new();
    }

    Task Because() => new DeferredSiloStart(_silo, NullLogger<Kernel>.Instance).StartAsync(_token.Token);

    void Destroy() => _token.Dispose();

    [Fact] Task should_not_forward_the_call() => _silo.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
}
