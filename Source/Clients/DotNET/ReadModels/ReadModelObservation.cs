// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Replays the latest collection while owning the lifetime of a dedicated read model stream.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
/// <param name="source">The stream to subscribe to when the first subscriber arrives.</param>
public sealed class ReadModelObservation<TReadModel>(IObservable<IEnumerable<TReadModel>> source) : ISubject<IEnumerable<TReadModel>>, IDisposable
{
    readonly object _gate = new();
    readonly ReplaySubject<IEnumerable<TReadModel>> _subject = new(1);
    readonly SingleAssignmentDisposable _upstream = new();
    int _subscribers;
    bool _started;
    bool _stopped;

    /// <inheritdoc/>
    public IDisposable Subscribe(IObserver<IEnumerable<TReadModel>> observer)
    {
        lock (_gate)
        {
            if (_stopped) return _subject.Subscribe(observer);

            _subscribers++;
            var subscription = _subject.Subscribe(observer);
            if (!_started)
            {
                _started = true;
                try
                {
                    _upstream.Disposable = source.Subscribe(OnNext, OnError, OnCompleted);
                }
                catch (Exception error)
                {
                    OnError(error);
                }
            }

            return Disposable.Create(() =>
            {
                lock (_gate)
                {
                    subscription.Dispose();
                    if (--_subscribers == 0) OnCompleted();
                }
            });
        }
    }

    /// <inheritdoc/>
    public void OnNext(IEnumerable<TReadModel> value)
    {
        lock (_gate)
        {
            if (!_stopped) _subject.OnNext(value);
        }
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        lock (_gate)
        {
            if (_stopped) return;
            _stopped = true;
            _upstream.Dispose();
            _subject.OnError(error);
        }
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
        lock (_gate)
        {
            if (_stopped) return;
            _stopped = true;
            _upstream.Dispose();
            _subject.OnCompleted();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        OnCompleted();
        _subject.Dispose();
    }
}
