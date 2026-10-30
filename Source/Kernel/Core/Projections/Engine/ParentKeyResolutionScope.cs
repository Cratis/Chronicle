// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Scopes the event-history boundary used to resolve a child's parent hierarchy.
/// </summary>
/// <remarks>
/// Ordinary resolution reads strictly before the original child event. A deferred child released by
/// a parent event reads up to that releasing event, inclusive, even when replay has already stored
/// later parent-key changes. Nested ancestors and sibling futures inherit the release boundary.
/// Disposing the scope restores the previous boundary in the current asynchronous flow.
/// </remarks>
public sealed class ParentKeyResolutionScope : IDisposable
{
    static readonly AsyncLocal<ParentKeyResolutionScope?> _current = new();
    readonly ParentKeyResolutionScope? _previous;

    ParentKeyResolutionScope(EventSequenceNumber sequenceNumber, bool inclusive)
    {
        _previous = _current.Value;
        SequenceNumber = sequenceNumber;
        Inclusive = inclusive;
        _current.Value = this;
    }

    /// <summary>
    /// Gets the boundary for the current asynchronous flow.
    /// </summary>
    internal static ParentKeyResolutionScope Current => _current.Value!;

    /// <summary>
    /// Gets the sequence number that bounds the parent lookup.
    /// </summary>
    internal EventSequenceNumber SequenceNumber { get; }

    /// <summary>
    /// Gets a value indicating whether the boundary event itself may be the parent.
    /// </summary>
    internal bool Inclusive { get; }

    /// <summary>
    /// Starts resolution of deferred children as of the releasing parent event, inclusive.
    /// </summary>
    /// <param name="sequenceNumber">The releasing parent event's sequence number, not the deferred child's.</param>
    /// <returns>A scope that restores the previous boundary when disposed.</returns>
    public static ParentKeyResolutionScope ForDeferredRelease(EventSequenceNumber sequenceNumber) => new(sequenceNumber, true);

    /// <inheritdoc/>
    public void Dispose() => _current.Value = _previous;

    /// <summary>
    /// Starts ordinary resolution strictly before the child event, unless an outer scope already set the boundary.
    /// </summary>
    /// <param name="sequenceNumber">The child event's sequence number.</param>
    /// <returns>A scope that restores the previous boundary when disposed.</returns>
    internal static ParentKeyResolutionScope ForChild(EventSequenceNumber sequenceNumber) =>
        new(_current.Value?.SequenceNumber ?? sequenceNumber, _current.Value?.Inclusive ?? false);
}
