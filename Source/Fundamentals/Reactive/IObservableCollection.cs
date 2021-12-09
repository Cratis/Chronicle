// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reactive
{
    /// <summary>
    /// Defines an observable collection.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public interface IObservableCollection<TItem> : ICollection<TItem>, IDisposable
    {
        /// <summary>
        /// The event that is fired if the collection is cleared.
        /// </summary>
        event ObservableCollectionCleared Cleared;

        /// <summary>
        /// Gets the added <see cref="IObservable{T}"/>.
        /// </summary>
        IObservable<TItem> Added { get; }

        /// <summary>
        /// Gets the removed <see cref="IObservable{T}"/>.
        /// </summary>
        IObservable<TItem> Removed { get; }
    }
}
