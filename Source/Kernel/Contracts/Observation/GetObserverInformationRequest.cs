// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the request for getting observer information for a specific observer.
/// </summary>
[ProtoContract]
public class GetObserverInformationRequest : IObserverCommand
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string ObserverId { get; set; }

    /// <inheritdoc/>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; } = string.Empty;
}
