// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

[DependencyInjection.IgnoreConvention]
public class UnseenProjection : IProjectionFor<UnseenProjectedModel>
{
    public ProjectionId Identifier => "unseen-projection";

    public void Define(IProjectionBuilderFor<UnseenProjectedModel> builder) => builder
        .From<UnseenEvent>(from => from.Set(model => model.Name).To(evt => evt.Name));
}
