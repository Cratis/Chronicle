// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Converts between <see cref="Contracts.Events.EventMetadata"/> and <see cref="EventMetadata"/>.
/// </summary>
internal static class EventMetadataConverters
{
    /// <summary>
    /// Converts a <see cref="Contracts.Events.EventMetadata"/> to an <see cref="EventMetadata"/>.
    /// </summary>
    /// <param name="metadata">The event metadata to convert.</param>
    /// <returns>The converted event metadata.</returns>
    public static EventMetadata ToApi(this Contracts.Events.EventMetadata metadata) => new(
            metadata.SequenceNumber,
            metadata.Type.ToApi());

    /// <summary>
    /// Converts an <see cref="EventMetadata"/> to a <see cref="Contracts.Events.EventMetadata"/>.
    /// </summary>
    /// <param name="metadata">The event metadata to convert.</param>
    /// <returns>The converted event metadata.</returns>
    public static Contracts.Events.EventMetadata ToContract(this EventMetadata metadata) => new()
    {
        SequenceNumber = metadata.SequenceNumber,
        Type = metadata.Type.ToContract()
    };
}
