// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.AccountHolders;

namespace Integration.AccountHolders;

public record AccountHolder(string FirstName, string LastName, DateTime DateOfBirth, string SocialSecurityNumber, Address Address);
