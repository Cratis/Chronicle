// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the response from initiating a replay.
/// </summary>
[ProtoContract]
public class ReplayResponse
{
    /// <summary>
    /// Gets or sets the identifier of the replay job that was started or resumed.
    /// </summary>
    /// <remarks>
    /// Empty when the observer is not replayable and no job was started.
    /// </remarks>
    [ProtoMember(1)]
    public string JobId { get; set; } = string.Empty;
}
