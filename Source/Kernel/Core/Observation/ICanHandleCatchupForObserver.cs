// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that can handle catchup for a specific observer.
/// </summary>
public interface ICanHandleCatchupForObserver
{
    /// <summary>
    /// Represents an error that can occur.
    /// </summary>
    public enum Error
    {
        /// <summary>
        /// Unknown error occurred.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Handler cannot handle for the observer.
        /// </summary>
        CannotHandle = 1,
    }

    /// <summary>
    /// Begin catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> BeginCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// Resume catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> ResumeCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// End catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> EndCatchupFor(ObserverDetails observerDetails);
}
