// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Specifications.Events;

/// <summary>
/// Represents a factory for creating <see cref="AppendedEvent"/> instances.
/// </summary>
public static class AppendedEventFactory
{
    /// <summary>
    /// Creates an <see cref="AppendedEvent"/> from a <see cref="EventSequenceNumber"/> and content.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to create the event for.</param>
    /// <param name="content">The actual instance of the event.</param>
    /// <returns>A new <see cref="AppendedEventForSpecifications"/> which also implements <see cref="AppendedEvent"/>.</returns>
    public static AppendedEventForSpecifications Create(EventSequenceNumber sequenceNumber, object content)
    {
        return new(
            EventMetadata.EmptyWithEventSequenceNumber(sequenceNumber),
            EventContext.Empty,
            content.AsExpandoObject(),
            content);
    }
}
