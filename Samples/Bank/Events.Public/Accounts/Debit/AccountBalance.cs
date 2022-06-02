// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Events.Public.Accounts.Debit;

[EventType("cf0a9242-706e-4293-bd3d-e795b9348bd6", isPublic: true)]
public record AccountBalance(double Amount);
