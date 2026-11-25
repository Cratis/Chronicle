// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

/// <summary>
/// A customer was registered; joined into every order that names the customer.
/// </summary>
/// <param name="Name">The customer's name.</param>
[EventType]
public record SharedCustomerRegistered(string Name);
