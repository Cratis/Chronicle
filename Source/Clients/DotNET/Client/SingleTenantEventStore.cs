// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="ISingleTenantEventStore"/>.
/// </summary>
public class SingleTenantEventStore : ISingleTenantEventStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleTenantEventStore"/> class.
    /// </summary>
    /// <param name="eventSequences"><see cref="IEventSequences"/> to use.</param>
    public SingleTenantEventStore(IEventSequences eventSequences)
    {
        Sequences = eventSequences;
    }

    /// <inheritdoc/>
    public IEventSequences Sequences { get; }
}
