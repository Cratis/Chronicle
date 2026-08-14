// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response with the event sequences a namespace has.
/// </summary>
[ProtoContract]
public class GetEventSequencesResponse
{
    /// <summary>
    /// Gets or sets the identifiers of the event sequences.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<string> EventSequenceIds { get; set; } = [];
}
