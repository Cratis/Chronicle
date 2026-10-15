// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

[EventType("read-protection-stock", generation: 2)]
public record StockSetV2(int Quantity);
