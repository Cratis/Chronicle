// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the typed outcome for one requested observer in an <see cref="AppliedThroughResponse"/>.
/// </summary>
[ProtoContract]
public class AppliedThroughObserverResult
{
    /// <summary>
    /// Gets or sets the observer identifier this outcome is for.
    /// </summary>
    [ProtoMember(1)]
    public string ObserverId { get; set; }

    /// <summary>
    /// Gets or sets the typed outcome for this observer.
    /// </summary>
    [ProtoMember(2)]
    public AppliedThroughOutcome Outcome { get; set; }
}
