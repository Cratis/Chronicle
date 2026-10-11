// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_generation_is_pinned;

public class PinnedPersonProjection : IProjectionFor<PinnedProjectedPerson>
{
    public void Define(IProjectionBuilderFor<PinnedProjectedPerson> builder) => builder.From<PinnedPersonRegistered>();
}
