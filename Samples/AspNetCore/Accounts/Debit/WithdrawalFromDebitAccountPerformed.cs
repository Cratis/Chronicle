// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Sample.Accounts.Debit
{
    [EventType("507a71d9-862f-4615-b8e8-2427d9568373")]
    public record WithdrawalFromDebitAccountPerformed(double Amount);
}
