// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Kernel.Integration.Projections.when_redacting_an_event;

public class ContentProjection : IProjectionFor<ContentReadModel>
{
    public void Define(IProjectionBuilderFor<ContentReadModel> builder) => builder
        .From<ContentChanged>(from => from.Set(model => model.Content).To(@event => @event.Content));
}
