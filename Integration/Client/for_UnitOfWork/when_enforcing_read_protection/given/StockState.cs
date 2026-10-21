// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

[Passive]
[FromEvent<StockSet>]
[RemovedWith<StockRemoved>]
public record StockState(Guid Id, int Quantity);
