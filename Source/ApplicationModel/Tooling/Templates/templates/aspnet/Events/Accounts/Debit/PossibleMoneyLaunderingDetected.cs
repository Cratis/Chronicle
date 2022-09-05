// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts;
using Concepts.Accounts;

namespace Events.Accounts.Debit;

[EventType("66740d58-cf08-4f30-b793-6d9a306a9eef")]
public record PossibleMoneyLaunderingDetected(PersonId PersonId, AccountId AccountId, DateOnly LastOccurrence);
