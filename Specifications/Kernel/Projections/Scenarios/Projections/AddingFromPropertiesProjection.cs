// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Projections;

public class AddingFromPropertiesProjection : IProjectionFor<Model>
{
    public ProjectionId Identifier => "1796eea2-26e9-4fda-ad3c-d4aa27d00000";

    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .Add(m => m.IntValue).With(e => e.IntValue)
            .Add(m => m.FloatValue).With(e => e.FloatValue)
            .Add(m => m.DoubleValue).With(e => e.DoubleValue));
}
