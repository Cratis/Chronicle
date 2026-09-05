// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the result of checking whether a named set of observers have applied through a target position.
/// </summary>
[ProtoContract]
public class AppliedThroughResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether every requested observer reached <see cref="AppliedThroughOutcome.Ready"/>.
    /// </summary>
    [ProtoMember(1)]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the typed outcome for every requested observer.
    /// </summary>
    [ProtoMember(2)]
    public IEnumerable<AppliedThroughObserverResult> Results { get; set; } = [];
}
