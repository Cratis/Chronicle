// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering;

/// <summary>
/// Holds every call to the projections manager's registration methods for as long as <see cref="Hold"/> says, the way a
/// kernel does that is busy with other work, so specs can see what a client experiences when the owning grain is saturated.
/// </summary>
public class BusyProjectionsManagerCalls : IIncomingGrainCallFilter
{
    int _reached;
    int _applying;
    int _mostApplying;

    /// <summary>
    /// Gets or sets how long each registration call is held before it runs.
    /// </summary>
    public TimeSpan Hold { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets how many registration calls reached the projections manager.
    /// </summary>
    public int Reached => _reached;

    /// <summary>
    /// Gets the most calls to the grain's own <c language="csharp">Register</c> that were running at the same time.
    /// </summary>
    public int MostApplying => _mostApplying;

    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (IsRegistration(context))
        {
            Interlocked.Increment(ref _reached);
            var isApply = string.Equals(context.InterfaceMethod.Name, "Register", StringComparison.Ordinal);
            if (isApply)
            {
                var running = Interlocked.Increment(ref _applying);
                InterlockedMax(ref _mostApplying, running);
            }

            try
            {
                if (Hold > TimeSpan.Zero)
                {
                    await Task.Delay(Hold);
                }

                await context.Invoke();
            }
            finally
            {
                if (isApply)
                {
                    Interlocked.Decrement(ref _applying);
                }
            }

            return;
        }

        await context.Invoke();
    }

    static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    static bool IsRegistration(IIncomingGrainCallContext context) =>
        string.Equals(context.InterfaceMethod?.DeclaringType?.Name, "IProjectionsManager", StringComparison.Ordinal) &&
        (string.Equals(context.InterfaceMethod.Name, "Register", StringComparison.Ordinal) || string.Equals(context.InterfaceMethod.Name, "RegisterShared", StringComparison.Ordinal));
}
