// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleReplayForObserver"/> for reducers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerReplayHandler"/> class.
/// </remarks>
public class ReducerReplayHandler : ICanHandleReplayForObserver
{
    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> BeginReplayFor(ObserverDetails observerDetails) => await PerformWork(observerDetails);

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> ResumeReplayFor(ObserverDetails observerDetails) => await PerformWork(observerDetails);

    /// <inheritdoc/>
    public async Task<Result<ICanHandleReplayForObserver.Error>> EndReplayFor(ObserverDetails observerDetails) => await PerformWork(observerDetails);

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Reducer;

    static Task<Result<ICanHandleReplayForObserver.Error>> PerformWork(ObserverDetails observerDetails)
    {
        return CanHandle(observerDetails)
            ? Task.FromResult(Result<ICanHandleReplayForObserver.Error>.Success())
            : Task.FromResult(Result.Failed(ICanHandleReplayForObserver.Error.CannotHandle));
    }
}
