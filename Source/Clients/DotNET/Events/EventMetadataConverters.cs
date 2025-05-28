// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="EventMetadata"/>.
/// </summary>
internal static class EventMetadataConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventMetadata"/>.
    /// </summary>
    /// <param name="metadata"><see cref="EventMetadata"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Events.EventMetadata"/>.</returns>
    internal static Contracts.Events.EventMetadata ToContract(this EventMetadata metadata)
    {
        return new()
        {
            SequenceNumber = metadata.SequenceNumber,
            Type = metadata.Type.ToContract()
        };
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventMetadata"/>.
    /// </summary>
    /// <param name="metadata"><see cref="Contracts.Events.EventMetadata"/> to convert.</param>
    /// <returns>Converted <see cref="EventMetadata"/>.</returns>
    internal static EventMetadata ToClient(this Contracts.Events.EventMetadata metadata)
    {
        return new(
            metadata.SequenceNumber,
            metadata.Type.ToClient());
    }
}
