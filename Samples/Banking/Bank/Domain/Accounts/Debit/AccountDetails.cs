// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;
using Concepts.Customers;

namespace Domain.Accounts.Debit;

public record AccountDetails(AccountName Name, CustomerId Owner, CardEnabledOnAccount IncludeCard);
