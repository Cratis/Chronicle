// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Moq;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer;

static class catch_up_continuations
{
    static readonly ConditionalWeakTable<TestKitSilo, HashSet<int>> _firedTimers = new();

    public static Task RunCatchUpContinuation(this TestKitSilo silo)
    {
        var registrations = silo.TimerRegistry.Mock.Invocations
            .Where(_ => string.Equals(_.Method.Name, "RegisterTimer", StringComparison.Ordinal) || string.Equals(_.Method.Name, "RegisterGrainTimer", StringComparison.Ordinal))
            .ToArray();
        var fired = _firedTimers.GetOrCreateValue(silo);
        var index = Enumerable.Range(0, registrations.Length)
            .FirstOrDefault(candidate => !fired.Contains(candidate) && IsCatchUpContinuation(registrations[candidate]), -1);
        if (index < 0)
        {
            throw new InvalidOperationException("no new catch-up continuation was scheduled");
        }
        fired.Add(index);
        return silo.TimerRegistry.FireAsync(index);
    }

    /// <summary>
    /// Orleans passes the original callback as the timer wrapper's state. Timer options alone also match alert turns.
    /// </summary>
    /// <param name="registration">The timer registration.</param>
    /// <returns>True if the registration is a catch-up continuation.</returns>
    static bool IsCatchUpContinuation(IInvocation registration) => registration.Arguments.OfType<Func<CancellationToken, Task>>()
        .Any(callback => callback.Method.Name.StartsWith("<ScheduleCatchUpContinuation>", StringComparison.Ordinal));
}
