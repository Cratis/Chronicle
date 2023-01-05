// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventSequences;

/// <summary>
/// Stream sequence token that tracks sequence number and event index.
/// </summary>
public class EventSequenceNumberToken : StreamSequenceToken
{
    long _sequenceNumber;
    int _eventIndex;

    /// <summary>
    /// Gets the number sequence number within the event log.
    /// </summary>
    public override long SequenceNumber
    {
        get => _sequenceNumber;
        protected set => _sequenceNumber = value;
    }

    /// <summary>
    /// Gets an event index - not used!.
    /// </summary>
    public override int EventIndex
    {
        get => _eventIndex;
        protected set => _eventIndex = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceNumberToken"/>.
    /// </summary>
    public EventSequenceNumberToken()
    {
        _sequenceNumber = (long)EventSequenceNumber.WarmUp.Value;
        _eventIndex = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceNumberToken"/>.
    /// </summary>
    /// <param name="sequenceNumber">The actual <see cref="EventSequenceNumber"/>.</param>
    public EventSequenceNumberToken(EventSequenceNumber sequenceNumber)
    {
        _sequenceNumber = (long)sequenceNumber.Value;
        _eventIndex = 0;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals((EventSequenceNumberToken)obj!);

    /// <inheritdoc/>
    public override bool Equals(StreamSequenceToken other)
    {
        return
            other is not null &&
            other is EventSequenceNumberToken token &&
            token.SequenceNumber == SequenceNumber &&
            token.EventIndex == EventIndex;
    }

    /// <inheritdoc/>
    public override int CompareTo(StreamSequenceToken other)
    {
        if (other == null)
            return 1;

        var difference = SequenceNumber.CompareTo(other.SequenceNumber);
        return difference != 0 ? difference : EventIndex.CompareTo(other.EventIndex);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => SequenceNumber.GetHashCode();

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Format("[EventLogSequenceNumberToken: SequenceNumber={0}]", SequenceNumber);
    }
}
