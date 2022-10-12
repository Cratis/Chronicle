// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Serialization;
using Concepts.Accounts;

namespace Read.AccountHolders;

[DerivedType("b67b4e5b-d192-404b-ba6f-9647202bd20e")]
public record CreditAccount(AccountId Id, AccountName Name, AccountType Type) : IAccount;
