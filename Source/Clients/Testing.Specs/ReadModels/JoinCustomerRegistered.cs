// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event registering a customer; lives on the customer's own event source and is joined into orders.
/// </summary>
/// <param name="Name">The customer's name.</param>
[EventType]
public record JoinCustomerRegistered(string Name);
