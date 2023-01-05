// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IQueueCacheCursor"/> when there are no events..
/// </summary>
public class EmptyEventSequenceQueueCacheCursor : IQueueCacheCursor
{
    /// <inheritdoc/>
    public IBatchContainer GetCurrent(out Exception exception)
    {
        exception = null!;
        return null!;
    }

    /// <inheritdoc/>
    public bool MoveNext() => false;

    /// <inheritdoc/>
    public void RecordDeliveryFailure()
    {
    }

    /// <inheritdoc/>
    public void Refresh(StreamSequenceToken token)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
