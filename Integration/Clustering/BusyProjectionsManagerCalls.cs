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

    /// <summary>
    /// Gets or sets how long each registration call is held before it runs.
    /// </summary>
    public TimeSpan Hold { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets how many registration calls reached the projections manager.
    /// </summary>
    public int Reached => _reached;

    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (IsRegistration(context))
        {
            Interlocked.Increment(ref _reached);
            if (Hold > TimeSpan.Zero)
            {
                await Task.Delay(Hold);
            }
        }

        await context.Invoke();
    }

    static bool IsRegistration(IIncomingGrainCallContext context) =>
        string.Equals(context.InterfaceMethod?.DeclaringType?.Name, "IProjectionsManager", StringComparison.Ordinal) &&
        (string.Equals(context.InterfaceMethod.Name, "Register", StringComparison.Ordinal) || string.Equals(context.InterfaceMethod.Name, "RegisterShared", StringComparison.Ordinal));
}
