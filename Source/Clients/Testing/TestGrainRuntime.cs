// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.Timers;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents a minimal <see cref="IGrainRuntime"/> for constructing grains outside an Orleans silo.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use.</param>
internal sealed class TestGrainRuntime(IServiceProvider serviceProvider) : IGrainRuntime
{
    /// <inheritdoc/>
    public IGrainFactory GrainFactory => throw new NotSupportedException("GrainFactory is not supported in test scenarios.");

    /// <inheritdoc/>
    public ITimerRegistry TimerRegistry => throw new NotSupportedException("TimerRegistry is not supported in test scenarios.");

    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <inheritdoc/>
    public SiloAddress SiloAddress => SiloAddress.Zero;

    /// <inheritdoc/>
    public string SiloIdentity => "TestSilo";

    /// <inheritdoc/>
    public void DeactivateOnIdle(IGrainContext grainContext)
    {
    }

    /// <inheritdoc/>
    public void DelayDeactivation(IGrainContext grainContext, TimeSpan timeSpan)
    {
    }

    /// <inheritdoc/>
    public IStorage<TGrainState> GetStorage<TGrainState>(IGrainContext grainContext) =>
        throw new NotSupportedException("GetStorage is not supported in test scenarios.");
}
