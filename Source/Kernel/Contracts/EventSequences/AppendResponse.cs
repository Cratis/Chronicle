// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Contracts.EventSequences;

#pragma warning disable MA0036

/// <summary>
/// Represents the response from appending an event.
/// </summary>
[ProtoContract]
public class AppendResponse
{
    /// <summary>
    /// Gets or sets the <see cref="CorrelationId"/> for the operation.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public Guid CorrelationId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the sequence number of the event that was appended, if successful.
    /// </summary>
    [ProtoMember(2)]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets the constraint violations.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<ConstraintViolation> ConstraintViolations { get; set; } = [];

    /// <summary>
    /// Gets a list of errors.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> Errors { get; set; } = [];

    /// <summary>
    /// Gets concurrency violations.
    /// </summary>
    [ProtoMember(5)]
    public ConcurrencyViolation? ConcurrencyViolation { get; set; }
}
