// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>A configured grant for one issuer-qualified external principal.</summary>
public class ExternalEventStoreAccess : ConfiguredEventStoreAccess
{
    /// <summary>Gets the exact issuer.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets the exact subject or client identifier. Issuer-wide entries are not supported.</summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>Gets whether the subject is a client identifier rather than a token subject.</summary>
    public bool IsClient { get; init; }
}
