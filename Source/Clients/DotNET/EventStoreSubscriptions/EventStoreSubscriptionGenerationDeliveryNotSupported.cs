// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// The exception that is thrown when a subscription builder does not support pinned generation delivery.
/// </summary>
public class EventStoreSubscriptionGenerationDeliveryNotSupported() : Exception("This event store subscription builder does not support pinned generation delivery.");
