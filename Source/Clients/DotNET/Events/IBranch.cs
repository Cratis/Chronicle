// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Defines a branch from <see cref="IEventLog"/>.
/// </summary>
public interface IBranch : IEventSequence
{
    /// <summary>
    /// Gets the identifier that identifies which type of branch this is.
    /// </summary>
    BranchTypeId Type { get; }

    /// <summary>
    /// Gets the unique identifier of the branch.
    /// </summary>
    BranchId Identifier { get; }

    /// <summary>
    /// Gets the date and time when the branch was started.
    /// </summary>
    DateTimeOffset Started { get; }

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> the branch was started from.
    /// </summary>
    EventSequenceNumber From { get; }

    /// <summary>
    /// Merge the branch into the <see cref="IEventLog"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Merge();
}
