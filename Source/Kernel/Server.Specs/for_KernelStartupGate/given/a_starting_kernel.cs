// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_KernelStartupGate.given;

public class a_starting_kernel : Specification
{
    protected DefaultHttpContext _context;
    internal KernelStartupGate _gate;
    internal KernelStartupState _state;
    protected CancellationTokenSource _started;
    protected CancellationTokenSource _stopping;
    protected bool _passedThrough;
    protected ChronicleOptions _options;

    void Establish()
    {
        _started = new();
        _stopping = new();
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(_started.Token);
        lifetime.ApplicationStopping.Returns(_stopping.Token);
        _state = new(lifetime);
        _options = new();
        _context = new();
        _context.Request.Path = "/connections/count";
        var options = Substitute.For<IOptions<ChronicleOptions>>();
        options.Value.Returns(_ => _options);
        _gate = new(
            _ =>
            {
                _passedThrough = true;
                return Task.CompletedTask;
            },
            _state,
            options);
    }

    void Destroy()
    {
        _started.Dispose();
        _stopping.Dispose();
    }
}
