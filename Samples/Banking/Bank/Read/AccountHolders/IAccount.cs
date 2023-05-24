// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;

namespace Read.AccountHolders;

public interface IAccount
{
    AccountId Id { get; }
    AccountName Name { get; }
    AccountType Type { get; }
}
