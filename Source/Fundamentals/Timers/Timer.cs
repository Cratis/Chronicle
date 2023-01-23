// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Timers;

/// <summary>
/// Represents an implementation of <see cref="ITimer"/>.
/// </summary>
public class Timer : ITimer
{
    readonly System.Threading.Timer _timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Timer"/> class.
    /// </summary>
    /// <param name="timer">The actual timer.</param>
    public Timer(System.Threading.Timer timer)
    {
        _timer = timer;
    }

    /// <inheritdoc/>
    public void Dispose() => _timer.Dispose();
}
