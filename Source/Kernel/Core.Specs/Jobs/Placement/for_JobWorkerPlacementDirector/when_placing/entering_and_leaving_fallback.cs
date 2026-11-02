// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Orleans.Runtime.Placement;

namespace Cratis.Chronicle.Jobs.Placement.for_JobWorkerPlacementDirector.when_placing;

public class entering_and_leaving_fallback : given.a_director
{
    void Establish()
    {
        _options.Clustering.Roles.Jobs = false;
        _context.GetCompatibleSilos(Arg.Any<PlacementTarget>()).Returns([_remote]);
    }

    async Task Because()
    {
        foreach (var roles in new ClusterRoles?[] { new() { Jobs = false }, new() { Jobs = false }, new(), new(), new() { Jobs = false }, new() { Jobs = false }, null, null })
        {
            _siloRoles.GetRoles(_remote).Returns(roles ?? Cratis.Monads.Option<ClusterRoles>.None());
            await _director.OnAddActivation(new JobWorkerPlacementStrategy(), default, _context);
        }
    }

    [Fact] void should_log_only_once_per_transition() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Warning).ShouldEqual(4);
}
