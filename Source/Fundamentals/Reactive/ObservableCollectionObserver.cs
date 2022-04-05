// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reactive;

/// <summary>
/// Represents an observer for a <see cref="IObservableCollection{T}"/>.
/// </summary>
public class ObservableCollectionObserver : IDisposable
{
    readonly Action _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableCollectionObserver"/> class.
    /// </summary>
    /// <param name="disposed">Action that gets called when disposed.</param>
    public ObservableCollectionObserver(Action disposed)
    {
        _disposed = disposed;
    }

    /// <inheritdoc/>
    public void Dispose() => _disposed();
}
