// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers.for_CpuBoundWorker.given;

public class TestGrain : Grain, ITestGrain
{
    public Task<string> Get()
    {
        return Task.FromResult("This is a test grain so that the test project has a grain that can be resolved (that isn't a sync-work grain) when setting up a test cluster");
    }
}
