// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="ICanHandleCatchupForObserver"/> for reducers.
/// </summary>
public class ReducerCatchupHandler : ICanHandleCatchupForObserver
{
    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> BeginCatchupFor(ObserverDetails observerDetails) => PerformWork(observerDetails);

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> ResumeCatchupFor(ObserverDetails observerDetails) => PerformWork(observerDetails);

    /// <inheritdoc/>
    public Task<Result<ICanHandleCatchupForObserver.Error>> EndCatchupFor(ObserverDetails observerDetails) => PerformWork(observerDetails);

    static bool CanHandle(ObserverDetails observerDetails) => observerDetails.Type == ObserverType.Reducer;

    static Task<Result<ICanHandleCatchupForObserver.Error>> PerformWork(ObserverDetails observerDetails)
    {
        return CanHandle(observerDetails)
            ? Task.FromResult(Result<ICanHandleCatchupForObserver.Error>.Success())
            : Task.FromResult(Result.Failed(ICanHandleCatchupForObserver.Error.CannotHandle));
    }
}
