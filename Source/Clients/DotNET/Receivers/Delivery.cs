// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Represents a decoded batch ready for dispatch.
/// </summary>
/// <param name="Target">The observer and tenant.</param>
/// <param name="Partition">The partition.</param>
/// <param name="Events">The events to dispatch.</param>
/// <param name="InitialState">The optional reducer state as JSON.</param>
internal record Delivery(DeliveryTarget Target, string Partition, IReadOnlyList<AppendedEvent> Events, string? InitialState);
