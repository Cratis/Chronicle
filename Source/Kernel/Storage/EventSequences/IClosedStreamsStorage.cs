// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Defines a storage provider for the closed streams.
/// </summary>
public interface IClosedStreamsStorage
{
    /// <summary>
    /// The types of errors that can occur when saving.
    /// </summary>
    public enum SaveErrors
    {
        /// <summary>
        /// An unknown error.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The stream has already been closed before.
        /// </summary>
        StreamAlreadyClosed = 1
    }

    /// <summary>
    /// Saves the closed stream state.
    /// </summary>
    /// <param name="closedStreamId">The <see cref="ClosedStreamId"/>.</param>
    /// <param name="reason">The <see cref="ClosedStreamReason"/>.</param>
    /// <returns><see cref="Result{T}"/> of <see cref="SaveErrors"/>.</returns>
    Task<Result<SaveErrors>> Save(ClosedStreamId closedStreamId, ClosedStreamReason reason);

    /// <summary>
    /// Gets the closed stream state if the stream has been closed.
    /// </summary>
    /// <param name="closedStreamId">The <see cref="ClosedStreamId"/>.</param>
    /// <returns><see cref="Option{T}"/> of <see cref="ClosedStreamState"/>.</returns>
    Task<Option<ClosedStreamState>> Get(ClosedStreamId closedStreamId);
}
