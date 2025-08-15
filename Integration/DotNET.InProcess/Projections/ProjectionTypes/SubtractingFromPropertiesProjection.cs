// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;

public class SubtractingFromPropertiesProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .Subtract(m => m.IntValue).With(e => e.IntValue)
            .Subtract(m => m.FloatValue).With(e => e.FloatValue)
            .Subtract(m => m.DoubleValue).With(e => e.DoubleValue));
}
