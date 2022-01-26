// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RCS1110, CA1050, MA0047

/// <summary>
/// Represents a simple logger for looking at timings. It measures from last call and outputs delta.
/// </summary>
public static class TimedLogging
{
    static DateTime _previous = DateTime.UtcNow;

    /// <summary>
    /// Write message to console.
    /// </summary>
    /// <param name="message">Message to write.</param>
    public static void Write(string message)
    {
        var now = DateTime.UtcNow;
        var delta = now.Subtract(_previous);

        Console.WriteLine($"{delta.Milliseconds,4} - {message}");
        _previous = now;
    }
}
