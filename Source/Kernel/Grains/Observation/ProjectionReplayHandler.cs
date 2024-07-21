// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for projections.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
/// </remarks>
public class ProjectionReplayHandler : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public Task<bool> CanHandle(ObserverDetails observerDetails) => Task.FromResult(observerDetails.Type == ObserverType.Projection);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        // TODO: Get projection instance and start replay
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        // TODO: Get projection instance and start replay
        throw new NotImplementedException();
    }
}
