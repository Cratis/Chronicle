// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.ProjectionTypes;

public class FromAllWithDynamicDictionaryKeyProjection : IProjectionFor<EventCountByType>
{
    public void Define(IProjectionBuilderFor<EventCountByType> builder) => builder
        .FromAll(_ => _
            .Count(m => m.CountsByEventTypeId, ctx => ctx.EventType.Id));
}
