// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionPipeline.given
{
    public class all_dependencies : Specification
    {
        protected Mock<IProjectionEventProvider> event_provider;
        protected Mock<IProjection> projection;

        void Establish()
        {
            event_provider = new();
            projection = new();
        }
    }
}
