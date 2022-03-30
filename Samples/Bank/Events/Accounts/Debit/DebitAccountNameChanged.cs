// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;

namespace Events.Accounts.Debit
{
    [EventType("d046516e-f13f-4c65-b03b-aa54c92b5f55")]
    public record DebitAccountNameChanged(AccountName Name);
}
