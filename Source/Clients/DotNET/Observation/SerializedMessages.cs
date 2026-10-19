// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Wraps the unsynchronized message subject of an observer so results from concurrent deliveries are published
/// one at a time, and publishing after disposal is ignored.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
/// <param name="initial">The initial message.</param>
internal sealed class SerializedMessages<TMessage>(TMessage initial) : IDisposable
{
    readonly BehaviorSubject<TMessage> _subject = new(initial);
#if NET8_0
    readonly object _lock = new();
#else
    readonly Lock _lock = new();
#endif
    bool _disposed;

    /// <summary>
    /// Gets the observable of messages.
    /// </summary>
    public IObservable<TMessage> Messages => _subject;

    /// <summary>
    /// Publishes a message, unless already disposed.
    /// </summary>
    /// <param name="message">The message.</param>
    public void Publish(TMessage message)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _subject.OnNext(message);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subject.Dispose();
        }
    }
}
