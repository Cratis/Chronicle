// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Patterns;

/// <summary>
/// Represents the request for getting the patterns that apply to a context.
/// </summary>
[ProtoContract]
public class GetPatternsRequest
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
    /// Gets or sets the scope to get patterns within.
    /// </summary>
    /// <remarks>
    /// The scope is a field of its own rather than an entry in <see cref="Context"/>: it selects which behavior is
    /// being asked about, whereas the context describes the situation. Folding the two together would make it
    /// possible to ask for a pattern with no scope at all, which has no answer.
    /// </remarks>
    [ProtoMember(3)]
    public string GroupingKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the partial context to match against, keyed by facet name.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IDictionary<string, string> Context { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the lowest confidence a returned pattern may hold.
    /// </summary>
    [ProtoMember(5)]
    public double MinimumConfidence { get; set; }

    /// <summary>
    /// Gets or sets the largest number of patterns to return.
    /// </summary>
    [ProtoMember(6)]
    public int MaximumResults { get; set; }
}
