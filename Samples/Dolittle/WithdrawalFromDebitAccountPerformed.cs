// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.SDK.Events;

namespace Sample
{
    [EventType("507a71d9-862f-4615-b8e8-2427d9568373")]
    public record WithdrawalFromDebitAccountPerformed(double amount);
}
