// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// A kernel whose dormant-observer safety net looks every few seconds instead of every ten minutes, and that records the
/// calls made to its grains. Kept apart from <see cref="DormantObserverFixture"/> because the interval is a setting of the
/// kernel, and the other specifications need dormant observers to stay undisturbed.
/// </summary>
public class DormantSafetyNetFixture : DormantObserverFixture
{
    /// <summary>
    /// Gets the recorder of the calls made to the grains of the kernel.
    /// </summary>
    public GrainCallRecorder Calls { get; } = new();
}
