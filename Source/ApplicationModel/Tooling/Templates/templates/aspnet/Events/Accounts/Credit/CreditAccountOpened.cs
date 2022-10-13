// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts;
using Concepts.Accounts;

namespace Events.Accounts.Credit;

[EventType("0678d2a6-6c40-4d2a-be2c-eed2cf421c92")]
public record CreditAccountOpened(AccountName Name, PersonId Owner, bool IncludeCard);
