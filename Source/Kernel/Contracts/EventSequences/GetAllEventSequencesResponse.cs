// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response for getting all event sequences.
/// </summary>
[ProtoContract]
public class GetAllEventSequencesResponse
{
    /// <summary>
    /// Gets or sets the collection of event sequences.
    /// </summary>
    [ProtoMember(1)]
    public IList<EventSequenceInformation> EventSequences { get; set; } = [];
}
