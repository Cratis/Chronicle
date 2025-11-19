// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;

public class CompositeKeyFromPropertyAndContextPropertyProjection : IProjectionFor<ReadModelWithCompositeKey>
{
    public void Define(IProjectionBuilderFor<ReadModelWithCompositeKey> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .UsingCompositeKey<CompositeKey>(_ => _
                .Set(k => k.First).To(e => e.StringValue)
                .Set(k => k.Second).ToEventContextProperty(c => c.SequenceNumber))
            .Set(m => m.LastUpdated).ToEventContextProperty(e => e.Occurred));
}
