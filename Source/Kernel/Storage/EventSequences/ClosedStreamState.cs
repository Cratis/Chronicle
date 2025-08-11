// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents the persisted state of a closed stream. This state is meant to have a composite identifier consisting of the
/// <see cref="EventSequenceId"/>, <see cref="EventSourceId"/>, <see cref="EventStreamType"/> and <see cref="EventStreamId"/>.
/// </summary>
public class ClosedStreamState
{
    /// <summary>
    /// Gets the <see cref="ClosedStreamReason"/>.
    /// </summary>
    public ClosedStreamReason Reason { get; init; } = ClosedStreamReason.Unspecified;
}

/*
    EventSourceType : BankAccount
    EventSourceId : <Bank account number>
    (
        EventStreamType : Transactions
        EventStreamId : Year-2024 => Close => New Stream Id (Year-2025)
    )
*/
