// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;

/// <summary>
/// A read model that is removed using an empty event that relies on implicit EventSourceId resolution.
/// </summary>
/// <param name="Id">The unique identifier of the item.</param>
/// <param name="Name">The name of the item.</param>
[FromEvent<ItemDefinedWithEmptyRemoval>]
[RemovedWith<ItemRemovedWithEmpty>]
public record ItemWithEmptyRemovalReadModel(
    Guid Id,
    string Name);
