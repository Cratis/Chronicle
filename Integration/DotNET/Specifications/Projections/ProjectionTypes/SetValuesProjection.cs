// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;

public class SetValuesProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
        .From<EmptyEvent>(_ => _
            .Set(m => m.StringValue).ToValue(KnownValues.StringValue)
            .Set(m => m.BoolValue).ToValue(KnownValues.BoolValue)
            .Set(m => m.IntValue).ToValue(KnownValues.IntValue)
            .Set(m => m.FloatValue).ToValue(KnownValues.FloatValue)
            .Set(m => m.DoubleValue).ToValue(KnownValues.DoubleValue)
            .Set(m => m.EnumValue).ToValue(KnownValues.EnumValue)
            .Set(m => m.GuidValue).ToValue(KnownValues.GuidValue)
            .Set(m => m.DateTimeValue).ToValue(KnownValues.DateTimeValue)
            .Set(m => m.DateOnlyValue).ToValue(KnownValues.DateOnlyValue)
            .Set(m => m.TimeOnlyValue).ToValue(KnownValues.TimeOnlyValue)
            .Set(m => m.DateTimeOffsetValue).ToValue(KnownValues.DateTimeOffsetValue)
            .Set(m => m.StringConceptValue).ToValue(KnownValues.StringConceptValue)
            .Set(m => m.BoolConceptValue).ToValue(KnownValues.BoolConceptValue)
            .Set(m => m.IntConceptValue).ToValue(KnownValues.IntConceptValue)
            .Set(m => m.FloatConceptValue).ToValue(KnownValues.FloatConceptValue)
            .Set(m => m.DoubleConceptValue).ToValue(KnownValues.DoubleConceptValue)
            .Set(m => m.GuidConceptValue).ToValue(KnownValues.GuidConceptValue));
}
