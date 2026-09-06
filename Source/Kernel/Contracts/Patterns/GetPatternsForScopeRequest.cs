// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Patterns;

/// <summary>
/// Represents the request for getting every pattern held for a scope.
/// </summary>
[ProtoContract]
public class GetPatternsForScopeRequest
{
    /// <summary>
    /// Gets or sets the event store to get patterns for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace to get patterns for.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope to get patterns for.
    /// </summary>
    [ProtoMember(3)]
    public string GroupingKey { get; set; } = string.Empty;
}
