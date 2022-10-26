// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;
using Concepts.Customers;

namespace Events.Accounts.Debit;

[EventType("3daa0bf9-4cca-455e-87bc-c27dade3eb11")]
public record DebitAccountOpened(AccountName Name, CustomerId Owner, bool IncludeCard);
