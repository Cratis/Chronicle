// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

public static class TimeSpanFactory
{
    /// <summary>
    /// Creates a default timeout for specs.
    /// </summary>
    /// <returns><see cref="TimeSpan"/>.</returns>
    public static TimeSpan DefaultTimeout()
    {
        if (Debugger.IsAttached)
        {
            return TimeSpan.MaxValue;
        }
        return TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Creates a specific timeout for specs.
    /// </summary>
    /// <param name="seconds">Number of seconds. Defaults to 5.</param>
    /// <returns><see cref="TimeSpan"/>.</returns>
    public static TimeSpan FromSeconds(int seconds = 5)
    {
        if (Debugger.IsAttached)
        {
            return TimeSpan.MaxValue;
        }
        return TimeSpan.FromSeconds(seconds);
    }
}
