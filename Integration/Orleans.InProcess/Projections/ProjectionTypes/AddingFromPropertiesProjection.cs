// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.ProjectionTypes;

public class AddingFromPropertiesProjection : IProjectionFor<Model>
{
    public void Define(IProjectionBuilderFor<Model> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .Add(m => m.IntValue).With(e => e.IntValue)
            .Add(m => m.FloatValue).With(e => e.FloatValue)
            .Add(m => m.DoubleValue).With(e => e.DoubleValue));
}
