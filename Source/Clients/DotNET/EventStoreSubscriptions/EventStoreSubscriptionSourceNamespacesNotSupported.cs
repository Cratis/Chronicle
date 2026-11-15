// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Exception that gets thrown when an event store subscription builder does not support restricting the source namespaces.
/// </summary>
public class EventStoreSubscriptionSourceNamespacesNotSupported() : Exception("This event store subscription builder does not support restricting the source namespaces.");
