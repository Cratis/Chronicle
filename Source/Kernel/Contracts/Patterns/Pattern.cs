// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Patterns;

/// <summary>
/// Represents a recurring combination of facets that was mined from event history.
/// </summary>
[ProtoContract]
public class Pattern
{
    /// <summary>
    /// Gets or sets the scope the pattern belongs to.
    /// </summary>
    [ProtoMember(1)]
    public string GroupingKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the facets the pattern is expressed in, keyed by facet name.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<string, string> Facets { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets how often the pattern holds when its context is present, in the range 0 to 1.
    /// </summary>
    [ProtoMember(3)]
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the share of all observed events the pattern was seen in, in the range 0 to 1.
    /// </summary>
    [ProtoMember(4)]
    public double Support { get; set; }

    /// <summary>
    /// Gets or sets how many times the pattern has been observed.
    /// </summary>
    [ProtoMember(5)]
    public long Occurrences { get; set; }

    /// <summary>
    /// Gets or sets the recency-weighted strength of the pattern.
    /// </summary>
    /// <remarks>
    /// Carried alongside <see cref="Occurrences"/> because the two answer different questions: occurrences is how
    /// often this has ever happened, weight is how much of that is still recent. A caller ordering by recency
    /// needs it, and leaving it off the wire would make the client's copy of the pattern quietly incomplete.
    /// </remarks>
    [ProtoMember(6)]
    public double Weight { get; set; }

    /// <summary>
    /// Gets or sets when the pattern was first observed.
    /// </summary>
    [ProtoMember(7)]
    public SerializableDateTimeOffset FirstSeen { get; set; }

    /// <summary>
    /// Gets or sets when the pattern was last observed.
    /// </summary>
    [ProtoMember(8)]
    public SerializableDateTimeOffset LastSeen { get; set; }
}
