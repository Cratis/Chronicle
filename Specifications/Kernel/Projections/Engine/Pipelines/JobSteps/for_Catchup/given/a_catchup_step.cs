// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Catchup.given;

public class a_catchup_step : all_dependencies
{
    protected Catchup catchup;

    void Establish() =>
        catchup = new(
            pipeline.Object,
            positions.Object,
            provider.Object,
            handler.Object,
            configuration,
            Mock.Of<ILogger<Catchup>>());
}
