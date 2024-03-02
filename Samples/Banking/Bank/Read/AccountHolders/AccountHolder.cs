// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.AccountHolders;

namespace Read.AccountHolders;

public record AccountHolder(string FirstName, string LastName, string SocialSecurityNumber, Address Address, DateTimeOffset LastUpdated);
