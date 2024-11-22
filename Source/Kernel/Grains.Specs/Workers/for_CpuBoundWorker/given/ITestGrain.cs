// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers.for_CpuBoundWorker.given;

[Alias("Cratis.Chronicle.Grains.Workers.for_CpuBoundWorker.given.ITestGrain")]
public interface ITestGrain : IGrainWithGuidKey
{
    [Alias("Get")]
    Task<string> Get();
}
