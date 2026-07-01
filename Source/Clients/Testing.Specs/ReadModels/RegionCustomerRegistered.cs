// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event registering a customer that is joined into orders. Carries a <see cref="Region"/> (which
/// collides by name with the order's own region) and a <see cref="City"/> (the value a join intends to
/// pull in). Lives on the customer's own event source.
/// </summary>
/// <param name="Region">The customer's region — a name collision with the order's region.</param>
/// <param name="City">The customer's city.</param>
[EventType]
public record RegionCustomerRegistered(string Region, string City);
