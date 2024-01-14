// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Projections;

public class CountingEventsProjection : IProjectionFor<Model>
{
    public ProjectionId Identifier => "1796eea2-26e9-4fda-ad3c-d4aa27d00000";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .Count(m => m.IntValue));
}
