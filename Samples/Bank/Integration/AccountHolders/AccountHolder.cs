// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Integration.AccountHolders;

public record AccountHolder(string FirstName, string LastName, DateTime DateOfBirth, string SocialSecurityNumber, string Address, string City, string PostalCode, string Country);
