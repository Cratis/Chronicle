// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Defines a single tenant client.
/// </summary>
public interface ISingleTenantClient : ICratisClient
{
    /// <summary>
    /// Gets the <see cref="ISingleTenantEventStore"/>.
    /// </summary>
    ISingleTenantEventStore EventStore { get; }
}
