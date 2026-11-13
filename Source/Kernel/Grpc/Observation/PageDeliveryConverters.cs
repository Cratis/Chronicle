// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Extension methods for converting a <see cref="PageDelivery"/> a client declared to a <see cref="PageDeliveryDefinition"/>.
/// </summary>
internal static class PageDeliveryConverters
{
    /// <summary>
    /// Convert a <see cref="PageDelivery"/> to a <see cref="PageDeliveryDefinition"/>.
    /// </summary>
    /// <param name="pageDelivery">The <see cref="PageDelivery"/> the client declared, if any.</param>
    /// <returns>The <see cref="PageDeliveryDefinition"/>, or null when the client did not enable page delivery.</returns>
    internal static PageDeliveryDefinition? ToChronicle(this PageDelivery? pageDelivery) =>
        pageDelivery is { Enabled: true }
            ? new PageDeliveryDefinition(
                (int)Math.Min(pageDelivery.MaxEventsPerPage, int.MaxValue),
                TimeSpan.FromMilliseconds(pageDelivery.MaxDelayMilliseconds),
                (int)Math.Min(pageDelivery.MaxBytesPerPage, int.MaxValue))
            : null;
}
