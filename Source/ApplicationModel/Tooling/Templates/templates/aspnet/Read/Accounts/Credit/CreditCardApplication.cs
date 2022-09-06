// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts.Credit;

namespace Read.Accounts.Debit;

public record CreditCardApplication(CreditCardApplicationId Identifier, DateTimeOffset Started);
