// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Projections;

public class CompositeKeyFromPropertyAndContextPropertyProjection : IProjectionFor<ModelWithCompositeKey>
{
    public ProjectionId Identifier => "1796eea2-26e9-4fda-ad3c-d4aa27d00000";

    public void Define(IProjectionBuilderFor<ModelWithCompositeKey> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .UsingCompositeKey<CompositeKey>(_ => _
                .Set(k => k.First).To(e => e.StringValue)
                .Set(k => k.Second).ToEventContextProperty(c => c.SequenceNumber))
            .Set(m => m.LastUpdated).ToEventContextProperty(e => e.Occurred));
}
