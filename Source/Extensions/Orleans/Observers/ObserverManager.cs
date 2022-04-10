// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Extensions.Orleans.Observers;

/// <summary>
/// Represents a system that maintains a collection of observers.
/// </summary>
/// <typeparam name="TObserver">Type of observer.</typeparam>
/// <remarks>
/// This is based on (copied with style adaptations) from the Orleans codebase.
/// </remarks>
public class ObserverManager<TObserver> : ObserverManager<IAddressable, TObserver>
    where TObserver : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverManager{TObserver}"/> class.
    /// </summary>
    /// <param name="expiration">The expiration for subscribers.</param>
    /// <param name="logger">Logger to use for logging.</param>
    /// <param name="loggerPrefix">Logger prefix.</param>
    public ObserverManager(TimeSpan expiration, ILogger logger, string loggerPrefix) : base(expiration, logger, loggerPrefix)
    {
    }
}

/// <summary>
/// Maintains a collection of observers.
/// </summary>
/// <typeparam name="TAddress">The address type.</typeparam>
/// <typeparam name="TObserver">The observer type.</typeparam>
public class ObserverManager<TAddress, TObserver> : IEnumerable<TObserver>
    where TAddress : notnull
    where TObserver : notnull
{
    class ObserverEntry
    {
        public TObserver Observer { get; set; }
        public DateTime LastSeen { get; set; }

        public ObserverEntry(TObserver observer, DateTime lastSeen)
        {
            Observer = observer;
            LastSeen = lastSeen;
        }
    }

    readonly string _loggerPrefix;

    readonly ConcurrentDictionary<TAddress, ObserverEntry> _observers = new();

    readonly ILogger _logger;

    /// <summary>
    /// Gets or sets the delegate used to get the date and time, for expiry.
    /// </summary>
    public Func<DateTime> GetDateTime { get; set; }

    /// <summary>
    /// Gets or sets the expiration time span, after which observers are lazily removed.
    /// </summary>
    public TimeSpan ExpirationDuration { get; set; }

    /// <summary>
    /// Gets the number of observers.
    /// </summary>
    public int Count => _observers.Count;

    /// <summary>
    /// Gets a copy of the observers.
    /// </summary>
    public IDictionary<TAddress, TObserver> Observers => _observers.ToDictionary(_ => _.Key, _ => _.Value.Observer);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverManager{TAddress,TObserver}"/> class.
    /// </summary>
    /// <param name="expiration">The expiration for subscribers.</param>
    /// <param name="logger">Logger to use for logging.</param>
    /// <param name="loggerPrefix">Logger prefix.</param>
    public ObserverManager(TimeSpan expiration, ILogger logger, string loggerPrefix)
    {
        ExpirationDuration = expiration;
        _logger = logger;
        _loggerPrefix = loggerPrefix;
        GetDateTime = () => DateTime.UtcNow;
    }

    /// <summary>
    /// Removes all observers.
    /// </summary>
    public void Clear()
    {
        _observers.Clear();
    }

    /// <summary>
    /// Ensures that the provided <paramref name="observer"/> is subscribed, renewing its subscription.
    /// </summary>
    /// <param name="address">The subscriber's address.</param>
    /// <param name="observer">The observer.</param>
    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    public void Subscribe(TAddress address, TObserver observer)
    {
        // Add or update the subscription.
        var now = GetDateTime();
        if (_observers.TryGetValue(address, out var entry))
        {
            entry.LastSeen = now;
            entry.Observer = observer;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.UpdatingEntry(_loggerPrefix, address, observer, _observers.Count);
            }
        }
        else
        {
            _observers[address] = new ObserverEntry(observer, now);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AddingEntry(_loggerPrefix, address, observer, _observers.Count);
            }
        }
    }

    /// <summary>
    /// Ensures that the provided <paramref name="subscriber"/> is unsubscribed.
    /// </summary>
    /// <param name="subscriber">The observer.</param>
    public void Unsubscribe(TAddress subscriber)
    {
        _logger.RemovingEntry(_loggerPrefix, subscriber, _observers.Count);
        _observers.TryRemove(subscriber, out var _);
    }

    /// <summary>
    /// Notifies all observers.
    /// </summary>
    /// <param name="notification">The notification delegate to call on each observer.</param>
    /// <param name="predicate">The predicate used to select observers to notify.</param>
    /// <returns>A <see cref="Task"/> representing the work performed.</returns>
    public async Task Notify(Func<TObserver, Task> notification, Func<TObserver, bool>? predicate = null)
    {
        var now = GetDateTime();
        var defunct = default(List<TAddress>);
        foreach (var observer in _observers)
        {
            if (observer.Value.LastSeen + ExpirationDuration < now)
            {
                // Expired observers will be removed.
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
                continue;
            }

            // Skip observers which don't match the provided predicate.
            if (predicate != null && !predicate(observer.Value.Observer))
            {
                continue;
            }

            try
            {
                await notification(observer.Value.Observer);
            }
            catch (Exception)
            {
                // Failing observers are considered defunct and will be removed..
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
            }
        }

        // Remove defunct observers.
        if (defunct != default(List<TAddress>))
        {
            foreach (var observer in defunct)
            {
                _observers.TryRemove(observer, out var _);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.RemovingDefunctEntry(_loggerPrefix, observer, _observers.Count);
                }
            }
        }
    }

    /// <summary>
    /// Notifies all observers which match the provided <paramref name="predicate"/>.
    /// </summary>
    /// <param name="notification">The notification delegate to call on each observer.</param>
    /// <param name="predicate">The predicate used to select observers to notify.</param>
    public void Notify(Action<TObserver> notification, Func<TObserver, bool>? predicate = null)
    {
        var now = GetDateTime();
        var defunct = default(List<TAddress>);
        foreach (var observer in _observers)
        {
            if (observer.Value.LastSeen + ExpirationDuration < now)
            {
                // Expired observers will be removed.
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
                continue;
            }

            // Skip observers which don't match the provided predicate.
            if (predicate != null && !predicate(observer.Value.Observer))
            {
                continue;
            }

            try
            {
                notification(observer.Value.Observer);
            }
            catch (Exception)
            {
                // Failing observers are considered defunct and will be removed..
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
            }
        }

        // Remove defunct observers.
        if (defunct != default(List<TAddress>))
        {
            foreach (var observer in defunct)
            {
                _observers.TryRemove(observer, out var _);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.RemovingDefunctEntry(_loggerPrefix, observer, _observers.Count);
                }
            }
        }
    }

    /// <summary>
    /// Removed all expired observers.
    /// </summary>
    public void ClearExpired()
    {
        var now = GetDateTime();
        var defunct = default(List<TAddress>);
        foreach (var observer in _observers)
        {
            if (observer.Value.LastSeen + ExpirationDuration < now)
            {
                // Expired observers will be removed.
                defunct ??= new List<TAddress>();
                defunct.Add(observer.Key);
            }
        }

        // Remove defunct observers.
        if (defunct?.Count > 0)
        {
            _logger.Info(_loggerPrefix + ": Removing {0} defunct observers entries.", defunct.Count);
            foreach (var observer in defunct)
            {
                _observers.TryRemove(observer, out var _);
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerator<TObserver> GetEnumerator()
    {
        return _observers.Select(observer => observer.Value.Observer).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
