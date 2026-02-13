// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for getting read model occurrences.
/// </summary>
[ProtoContract]
public class GetOccurrencesRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the read model.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the read model.
    /// </summary>
    [ProtoMember(3)]
    public ReadModelType Type { get; set; }
}
