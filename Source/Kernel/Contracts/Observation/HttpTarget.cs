// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the HTTP target for delivering events to an observer.
/// </summary>
[ProtoContract]
[ReservedProtoFields(3, 4, 5, 6, 7, 8, 9)] // Reserved for #5117.
public class HttpTarget
{
    /// <summary>
    /// Gets or sets the URL to deliver events to.
    /// </summary>
    [ProtoMember(1)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the headers to include when delivering events.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
