// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle;

/// <summary>
/// Factory for creating <see cref="TimeSpan"/> instances.
/// </summary>
public static class TimeSpanFactory
{
    const string TimeoutEnvVar = "CHRONICLE_TEST_TIMEOUT_SECONDS";
    const int DefaultTimeoutSeconds = 5;

    /// <summary>
    /// Creates a default timeout for specs.
    /// </summary>
    /// <remarks>
    /// Override with the CHRONICLE_TEST_TIMEOUT_SECONDS environment variable for CI environments.
    /// </remarks>
    /// <returns><see cref="TimeSpan"/>.</returns>
    public static TimeSpan DefaultTimeout()
    {
        if (Debugger.IsAttached)
        {
            return Timeout.InfiniteTimeSpan;
        }
        return TimeSpan.FromSeconds(GetTimeoutSeconds());
    }

    /// <summary>
    /// Creates a specific timeout for specs.
    /// </summary>
    /// <param name="seconds">Number of seconds. Defaults to 5.</param>
    /// <returns><see cref="TimeSpan"/>.</returns>
    public static TimeSpan FromSeconds(int seconds = DefaultTimeoutSeconds)
    {
        if (Debugger.IsAttached)
        {
            return Timeout.InfiniteTimeSpan;
        }
        return TimeSpan.FromSeconds(seconds);
    }

    static int GetTimeoutSeconds()
    {
        var envValue = Environment.GetEnvironmentVariable(TimeoutEnvVar);
        if (!string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out var seconds) && seconds > 0)
        {
            return seconds;
        }
        return DefaultTimeoutSeconds;
    }
}
