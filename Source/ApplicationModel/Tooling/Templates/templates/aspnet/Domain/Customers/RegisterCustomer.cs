// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Customers;

namespace Domain.Customers;

public record RegisterCustomer(CustomerId CustomerId, FirstName FirstName, LastName LastName, SocialSecurityNumber SocialSecurityNumber);
