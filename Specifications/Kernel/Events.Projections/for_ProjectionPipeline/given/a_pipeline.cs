// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.for_ProjectionPipeline.given
{
    public class a_pipeline : all_dependencies
    {
        protected ProjectionPipeline pipeline;

        void Establish() => pipeline = new (event_provider.Object, projection.Object, changeset_storage.Object, Mock.Of<ILogger<ProjectionPipeline>>());
    }
}
