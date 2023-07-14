// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Defines a multi tenant client.
/// </summary>
public interface IMultiTenantClient
{
    /// <summary>
    /// Gets the <see cref="IMultiTenantEventStore"/>.
    /// </summary>
    IMultiTenantEventStore EventStore { get; }
}
