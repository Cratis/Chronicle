// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Client;

/// <summary>
/// Defines a single tenant client.
/// </summary>
public interface ISingleTenantClient : IClient
{
    /// <summary>
    /// Gets the <see cref="ISingleTenantEventStore"/>.
    /// </summary>
    ISingleTenantEventStore EventStore { get; }
}
