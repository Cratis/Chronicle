// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Customers;

namespace Events.Customers;

[EventType("98c24abd-aadb-43b9-b0b6-3f18bff09840")]
public record CustomerRegistered(FirstName FirstName, LastName LastName, SocialSecurityNumber SocialSecurityNumber);
