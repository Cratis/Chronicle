// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Defines a single tenant event store.
/// </summary>
public interface ISingleTenantEventStore
{
    /// <summary>
    /// Gets the <see cref="IEventSequences"/>.
    /// </summary>
    IEventSequences Sequences { get; }
}
