// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Timers;

/// <summary>
/// Represents an implementation of <see cref="ITimer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Timer"/> class.
/// </remarks>
/// <param name="timer">The actual timer.</param>
public class Timer(System.Threading.Timer timer) : ITimer
{
    /// <inheritdoc/>
    public void Dispose() => timer.Dispose();
}
