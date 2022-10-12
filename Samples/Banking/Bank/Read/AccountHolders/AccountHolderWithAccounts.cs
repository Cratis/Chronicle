// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts;

namespace Read.AccountHolders;

public record AccountHolderWithAccounts(PersonId Id, string FirstName, string LastName, List<IAccount> Accounts);
