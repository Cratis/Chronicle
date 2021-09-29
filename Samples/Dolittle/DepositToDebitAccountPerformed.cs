// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.SDK.Events;

namespace Sample
{
    [EventType("adaab3e5-f797-4335-80d4-06758773f7e1")]
    public record DepositToDebitAccountPerformed(double amount);
}
