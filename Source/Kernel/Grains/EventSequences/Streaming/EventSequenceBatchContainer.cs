// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IBatchContainer"/> for MongoDB event sequence events.
/// </summary>
public class EventSequenceBatchContainer : IBatchContainer
{
    readonly IEnumerable<AppendedEvent> _events;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceBatchContainer"/> class.
    /// </summary>
    /// <param name="events">The <see cref="AppendedEvent"/>.</param>
    /// <param name="streamId">The identifier of the stream.</param>
    /// <param name="requestContext">The request context.</param>
    public EventSequenceBatchContainer(
        IEnumerable<AppendedEvent> events,
        StreamId streamId,
        IDictionary<string, object> requestContext)
    {
        _events = events;
        StreamId = streamId;
        AssociatedRequestContext = requestContext;
        if (_events.Any())
        {
            SequenceToken = new EventSequenceNumberToken(_events.First().Metadata.SequenceNumber);
        }
        else
        {
            SequenceToken = new EventSequenceNumberToken();
        }
    }

    /// <inheritdoc/>
    public StreamId StreamId { get; }

    /// <inheritdoc/>
    public StreamSequenceToken SequenceToken { get; }

    /// <summary>
    /// Gets the associated request context.
    /// </summary>
    public IDictionary<string, object> AssociatedRequestContext { get; }

    /// <inheritdoc/>
    public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>() =>
        _events.Select(_ => new Tuple<T, StreamSequenceToken>((T)(object)_, new EventSequenceNumberToken(_.Metadata.SequenceNumber))).ToArray();

    /// <inheritdoc/>
    public bool ImportRequestContext()
    {
        foreach (var (key, value) in AssociatedRequestContext)
        {
            RequestContext.Set(key, value);
        }
        return true;
    }
}
