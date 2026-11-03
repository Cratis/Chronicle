// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_reading_a_pinned_projection_by_id;

public class FirstGenerationPersonProjection : IProjectionFor<FirstGenerationPerson>
{
    public void Define(IProjectionBuilderFor<FirstGenerationPerson> builder) => builder.From<PinnedPersonRegisteredV1>();
}
