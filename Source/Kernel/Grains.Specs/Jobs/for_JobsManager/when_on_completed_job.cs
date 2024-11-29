// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_on_completed_job : given.the_manager
{
    static Exception _error;
    async Task Because() => _error = await Catch.Exception(async () => await _manager.OnCompleted("b28da2d8-b36a-4da4-9342-6aaf48da578e", JobStatus.None));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}