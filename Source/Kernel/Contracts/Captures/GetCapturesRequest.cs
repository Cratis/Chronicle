// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the request for getting captures.
/// </summary>
[ProtoContract]
public class GetCapturesRequest
{
    /// <summary>
    /// Gets or sets the event store to get captures for.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;
}
