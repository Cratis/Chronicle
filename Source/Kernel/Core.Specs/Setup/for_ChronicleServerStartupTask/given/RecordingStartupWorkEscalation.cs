// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

/// <summary>
/// Records what the background startup pass escalated, so specs assert through the seam instead of through logs.
/// </summary>
internal class RecordingStartupWorkEscalation : IStartupWorkEscalation
{
    readonly List<(StartupWorkItem Item, int Attempts, TimeSpan NextDelay)> _escalations = [];
    readonly List<(StartupWorkItem Item, int Attempts)> _recoveries = [];

    /// <summary>Gets the escalations recorded so far.</summary>
    internal IReadOnlyList<(StartupWorkItem Item, int Attempts, TimeSpan NextDelay)> Escalations
    {
        get
        {
            lock (_escalations)
            {
                return [.. _escalations];
            }
        }
    }

    /// <summary>Gets the recoveries recorded so far.</summary>
    internal IReadOnlyList<(StartupWorkItem Item, int Attempts)> Recoveries
    {
        get
        {
            lock (_recoveries)
            {
                return [.. _recoveries];
            }
        }
    }

    /// <inheritdoc/>
    public void Escalate(StartupWorkItem item, int attempts, TimeSpan nextDelay, Exception lastException)
    {
        lock (_escalations)
        {
            _escalations.Add((item, attempts, nextDelay));
        }
    }

    /// <inheritdoc/>
    public void Recovered(StartupWorkItem item, int attempts)
    {
        lock (_recoveries)
        {
            _recoveries.Add((item, attempts));
        }
    }
}
