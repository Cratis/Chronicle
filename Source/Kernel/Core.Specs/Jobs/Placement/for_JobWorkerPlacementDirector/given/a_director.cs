// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementDirector.given;

public class a_director : Cluster.given.a_role_placement
{
    protected JobWorkerPlacementDirector _director;
    protected ILogger<JobWorkerPlacementDirector> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<JobWorkerPlacementDirector>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _director = new(_eligibility, Options.Create(_options), _logger);
    }
}
