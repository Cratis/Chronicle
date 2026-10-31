// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents a stamp telling whether a sink may be holding writes in memory before they reach storage.
/// </summary>
/// <remarks>
/// An even value means no write can be buffered, so a write that has returned from the sink has been committed.
/// An odd value means writes may be buffered. The value changes on every open and close of a buffering window,
/// so comparing a stamp taken before and after an operation proves that no window was open at any point during it.
/// <see cref="Unknown"/> means the sink cannot tell, so callers must treat writes as possibly buffered.
/// </remarks>
/// <param name="Value">The raw stamp value; negative means unknown.</param>
public readonly record struct SinkBufferingWindow(long Value)
{
    /// <summary>
    /// Gets the stamp of a sink that cannot tell whether it buffers writes.
    /// </summary>
    public static SinkBufferingWindow Unknown { get; } = new(-1);

    /// <summary>
    /// Gets the stamp of a sink that never buffers writes.
    /// </summary>
    public static SinkBufferingWindow Closed { get; } = new(0);

    /// <summary>
    /// Gets a value indicating whether writes may be buffered, or it is not known.
    /// </summary>
    public bool IsOpen => Value < 0 || (Value & 1) == 1;

    /// <summary>
    /// Determines whether no buffering window was open at any point between two stamps.
    /// </summary>
    /// <param name="before">The stamp taken before the operation.</param>
    /// <param name="after">The stamp taken after the operation.</param>
    /// <returns>True only when both stamps are known, identical and closed.</returns>
    public static bool IsUnchangedAndClosed(SinkBufferingWindow before, SinkBufferingWindow after) =>
        before.Value >= 0 && before == after && (before.Value & 1) == 0;
}
