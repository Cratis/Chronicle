// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Counts the Entity Framework Core commands, connection opens and context initializations observed while it is alive.
/// </summary>
/// <remarks>
/// A zero count on every counter means the diagnostics did not fire, so the result is unmeasured rather than zero.
/// </remarks>
public sealed class EfCoreEventCounter : IObserver<DiagnosticListener>, IDisposable
{
    readonly IDisposable _allListeners;
    readonly List<IDisposable> _subscriptions = [];
    int _commands;
    int _connections;
    int _contexts;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreEventCounter"/> class and starts counting.
    /// </summary>
    public EfCoreEventCounter() => _allListeners = DiagnosticListener.AllListeners.Subscribe(this);

    /// <summary>
    /// Gets the commands executed.
    /// </summary>
    public int Commands => _commands;

    /// <summary>
    /// Gets the connections opened.
    /// </summary>
    public int Connections => _connections;

    /// <summary>
    /// Gets the contexts initialized.
    /// </summary>
    public int Contexts => _contexts;

    /// <inheritdoc/>
    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == "Microsoft.EntityFrameworkCore")
        {
            lock (_subscriptions)
            {
                _subscriptions.Add(value.Subscribe(new Events(this)));
            }
        }
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _allListeners.Dispose();
        lock (_subscriptions)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }
    }

    sealed class Events(EfCoreEventCounter owner) : IObserver<KeyValuePair<string, object?>>
    {
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key.EndsWith("CommandExecuted", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref owner._commands);
            }
            else if (value.Key.EndsWith("ConnectionOpened", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref owner._connections);
            }
            else if (value.Key.EndsWith("ContextInitialized", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref owner._contexts);
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}
