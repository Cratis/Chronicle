// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Handles at-least-once delivery of a typed trigger. Use effect receipts for Chronicle writes and effect keys for external calls.
/// </summary>
/// <typeparam name="TTrigger">The type marked with <see cref="TriggerAttribute"/>.</typeparam>
public interface IOccurrenceHandler<in TTrigger>
{
    /// <summary>
    /// Handles one leased attempt. Honor cancellation when the connection, lease or client lifetime ends.
    /// </summary>
    /// <param name="trigger">The recorded plaintext payload.</param>
    /// <param name="context">The durable identity and current attempt.</param>
    /// <param name="cancellationToken">Cancellation for the attempt.</param>
    /// <returns>The asynchronous handler work.</returns>
    Task Handle(TTrigger trigger, OccurrenceContext context, CancellationToken cancellationToken);
}
