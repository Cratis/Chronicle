// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="IMultiTenantEventStore"/>.
/// </summary>
public class MultiTenantEventStore : IMultiTenantEventStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenantEventStore"/> class.
    /// </summary>
    /// <param name="eventSequences"><see cref="IMultiTenantEventSequences"/> instance.</param>
    public MultiTenantEventStore(IMultiTenantEventSequences eventSequences) => Sequences = eventSequences;

    /// <inheritdoc/>
    public IMultiTenantEventSequences Sequences { get; }
}
