// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Measures how long each phase of one projection registration took, so a slow registration can be told apart
/// from a queued one when it times out for the caller.
/// </summary>
/// <param name="clock">The <see cref="TimeProvider"/> to measure with.</param>
sealed class RegistrationTiming(TimeProvider clock)
{
    readonly DateTimeOffset _started = clock.GetUtcNow();
    readonly Dictionary<RegistrationPhase, TimeSpan> _phases = [];
    DateTimeOffset _lastMark = clock.GetUtcNow();

    /// <summary>Gets or sets the number of definitions submitted.</summary>
    public int Definitions { get; set; }

    /// <summary>Gets or sets the number of definitions that needed work.</summary>
    public int Changed { get; set; }

    /// <summary>Gets or sets the number of subscriptions left to the background repair.</summary>
    public int Skipped { get; set; }

    /// <summary>Gets the total time taken.</summary>
    public TimeSpan Total { get; private set; }

    /// <summary>Gets the time spent detecting what changed.</summary>
    public TimeSpan Detecting => Phase(RegistrationPhase.Detecting);

    /// <summary>Gets the time spent registering with the engine.</summary>
    public TimeSpan Engine => Phase(RegistrationPhase.Engine);

    /// <summary>Gets the time spent subscribing.</summary>
    public TimeSpan Subscribing => Phase(RegistrationPhase.Subscribing);

    /// <summary>Gets the time spent retiring unregistered projections.</summary>
    public TimeSpan Retiring => Phase(RegistrationPhase.Retiring);

    /// <summary>
    /// Attributes the time since the previous mark to a phase.
    /// </summary>
    /// <param name="phase">The phase that just finished.</param>
    public void Mark(RegistrationPhase phase)
    {
        var now = clock.GetUtcNow();
        _phases[phase] = now - _lastMark;
        _lastMark = now;
    }

    /// <summary>
    /// Records the total time taken.
    /// </summary>
    public void Complete() => Total = clock.GetUtcNow() - _started;

    TimeSpan Phase(RegistrationPhase phase) => _phases.GetValueOrDefault(phase);
}
