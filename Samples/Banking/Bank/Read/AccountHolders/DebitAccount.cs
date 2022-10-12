// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Serialization;
using Concepts.Accounts;

namespace Read.AccountHolders;

[DerivedType("2c025801-2223-402c-a42a-893845bb1077")]
public record DebitAccount(AccountId Id, AccountName Name, AccountType Type) : IAccount;
