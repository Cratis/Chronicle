// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.InProcess.Integration.Projections.ProjectionTypes;

public class ConstantKeyProjection : IProjectionFor<ReadModel>
{
    public void Define(IProjectionBuilderFor<ReadModel> builder) => builder
        .From<EventWithPropertiesForAllSupportedTypes>(_ => _
            .UsingConstantKey(ConstantKeyProjection.ConstantKeyValue)
            .Set(m => m.StringValue).To(e => e.StringValue));

    public const string ConstantKeyValue = "constant-key";
}
