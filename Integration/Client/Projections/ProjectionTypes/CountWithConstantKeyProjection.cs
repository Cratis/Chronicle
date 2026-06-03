// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.ProjectionTypes;

public class CountWithConstantKeyProjection : IProjectionFor<CounterReadModel>
{
    public void Define(IProjectionBuilderFor<CounterReadModel> builder) => builder
        .From<EmptyEvent>(_ => _
            .UsingConstantKey(ConstantKeyValue)
            .Count(m => m.Count));

    public const string ConstantKeyValue = "count-constant-key";
}
