// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.SingleWriter;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelStartupState.given;

public class a_kernel_startup_state : Specification
{
    protected CancellationTokenSource _started;
    protected CancellationTokenSource _stopping;
    protected IHostApplicationLifetime _lifetime;
    protected ChronicleOptions _options;
    protected ISingleWriterLease _lease;
    internal KernelStartupState _state;

    void Establish()
    {
        _started = new();
        _stopping = new();
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _lifetime.ApplicationStarted.Returns(_started.Token);
        _lifetime.ApplicationStopping.Returns(_stopping.Token);
        _options = new();
        _options.Clustering.SingleWriterLease.Standby = true;
        _lease = Substitute.For<ISingleWriterLease>();
        _state = Server.given.startup_state.For(_lifetime, _options, _lease);
    }

    void Destroy()
    {
        _started.Dispose();
        _stopping.Dispose();
    }
}
