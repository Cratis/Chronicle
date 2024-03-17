// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Timers;

/// <summary>
/// Defines a factory for <see cref="ITimer"/>.
/// </summary>
public interface ITimerFactory
{
    /// <summary>
    /// Create a new timer.
    /// </summary>
    /// <param name="callback">A System.Threading.TimerCallback delegate representing a method to be executed.</param>
    /// <param name="dueTime">The amount of time to delay before callback is invoked, in milliseconds. Specify System.Threading.Timeout.Infinite to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
    /// <param name="period">The time interval between invocations of callback, in milliseconds. Specify System.Threading.Timeout.Infinite to disable periodic signaling.</param>
    /// <param name="state">Optionally an object containing information to be used by the callback method, or null.</param>
    /// <returns>A new <see cref="ITimer"/>.</returns>
    ITimer Create(TimerCallback callback, int dueTime, int period, object? state = null);
}
