// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Represents the request for getting seed data.
/// </summary>
[ProtoContract]
public class GetSeedDataRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace (optional, used for namespace-specific requests).
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;
}
