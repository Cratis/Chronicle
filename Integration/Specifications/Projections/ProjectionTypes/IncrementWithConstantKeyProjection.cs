// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;

public class IncrementWithConstantKeyProjection : IProjectionFor<CounterReadModel>
{
    public void Define(IProjectionBuilderFor<CounterReadModel> builder) => builder
        .From<EmptyEvent>(_ => _
            .UsingConstantKey(ConstantKeyValue)
            .Increment(m => m.Count));

    public const string ConstantKeyValue = "increment-constant-key";
}
