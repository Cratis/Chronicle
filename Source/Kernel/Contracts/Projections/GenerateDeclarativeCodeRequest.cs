// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for generating declarative C# projection code from projection declaration language.
/// </summary>
[ProtoContract]
public class GenerateDeclarativeCodeRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the projection declaration language representation of the projection.
    /// </summary>
    [ProtoMember(3)]
    public string Declaration { get; set; } = string.Empty;
}
