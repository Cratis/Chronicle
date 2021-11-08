// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Events.Projections.Changes;

namespace Cratis.Events.Projections.for_ProjectionPipeline.given
{
    public class all_dependencies : Specification
    {
        protected Mock<IProjectionEventProvider> event_provider;
        protected Mock<IChangesetStorage> changeset_storage;
        protected Mock<IProjection> projection;

        void Establish()
        {
            event_provider = new();
            projection = new();
            changeset_storage = new();
            event_provider.Setup(_ => _.ProvideFor(projection.Object)).Returns(new Subject<Event>());
        }
    }
}
