// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Concepts.AccountHolders;

namespace Read.AccountHolders;

public record AccountHolderWithAccounts(AccountHolderId Id, string FirstName, string LastName, Collection<IAccount> Accounts);
