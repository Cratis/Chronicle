// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
public class Jobs(IGrainFactory grainFactory) : IJobs
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetAll(GetAllRequest request, CallContext context = default)
    {
        var key = new JobsManagerKey(request.EventStoreName, request.Namespace);
        var manager = grainFactory.GetGrain<IJobsManager>(0, key);
        return (await manager.GetAllJobs()).ToContract();
    }
}
