// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Publishes the ambient <see cref="ObserverDeliveryContext"/> and enters the registered <see cref="IObserverDeliveryScope"/> hooks.
/// </summary>
/// <remarks>
/// This is a struct so that the path without registered hooks allocates nothing of its own: the hooks are resolved once
/// per root provider and cached, and the disposables of entered hooks are only collected when there are hooks.
/// A default value is inert and does nothing when disposed. Dispose it exactly once, after the delivery's service scope is disposed.
/// </remarks>
internal readonly struct ObserverDelivery : IDisposable
{
    static readonly ConditionalWeakTable<IServiceProvider, Lazy<IObserverDeliveryScope[]>> _scopesByProvider = new();
    static readonly IObserverDeliveryScope[] _noScopes = [];

    readonly ObserverDeliveryContext? _previous;
    readonly IDisposable[]? _entered;
    readonly bool _active;

    ObserverDelivery(ObserverDeliveryContext? previous, IDisposable[]? entered)
    {
        _previous = previous;
        _entered = entered;
        _active = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_active)
        {
            return;
        }

        if (_entered is null)
        {
            ObserverDeliveryContext.Current = _previous;
            return;
        }

        Leave(_entered, _entered.Length, _previous);
    }

    /// <summary>
    /// Enters a delivery synchronously in the caller's flow.
    /// </summary>
    /// <param name="serviceProvider">The root <see cref="IServiceProvider"/> the hooks are resolved from.</param>
    /// <param name="context">The <see cref="ObserverDeliveryContext"/> of the delivery.</param>
    /// <returns>The <see cref="ObserverDelivery"/> to dispose once the delivery's service scope is disposed.</returns>
    internal static ObserverDelivery Enter(IServiceProvider serviceProvider, ObserverDeliveryContext context)
    {
        var previous = ObserverDeliveryContext.Current;
        ObserverDeliveryContext.Current = context;

        var scopes = ScopesOf(serviceProvider);
        if (scopes.Length == 0)
        {
            return new(previous, null);
        }

        var entered = new IDisposable[scopes.Length];
        var count = 0;
        try
        {
            for (; count < scopes.Length; count++)
            {
                entered[count] = scopes[count].Enter(context);
            }
        }
        catch
        {
            Leave(entered, count, previous);
            throw;
        }

        return new(previous, entered);
    }

    /// <summary>
    /// Enters a flow that is explicitly outside any delivery, so that <see cref="ObserverDeliveryContext.Current"/> is null until disposed.
    /// </summary>
    /// <returns>The <see cref="ObserverDelivery"/> to dispose to restore the previous context.</returns>
    internal static ObserverDelivery EnterCleared()
    {
        var previous = ObserverDeliveryContext.Current;
        ObserverDeliveryContext.Current = null;
        return new(previous, null);
    }

    /// <summary>
    /// Runs long-lived background work on the thread pool without capturing the execution context of the caller,
    /// so that the work never inherits the <see cref="ObserverDeliveryContext"/> of the delivery that happened to start it.
    /// </summary>
    /// <param name="work">The work to run.</param>
    /// <returns>The task representing the work.</returns>
    internal static Task RunDetached(Func<Task> work)
    {
        if (ExecutionContext.IsFlowSuppressed())
        {
            return Task.Run(work);
        }

        Task started;
        using (ExecutionContext.SuppressFlow())
        {
            started = Task.Run(work);
        }

        return started;
    }

    static IObserverDeliveryScope[] ScopesOf(IServiceProvider serviceProvider) =>
        _scopesByProvider.TryGetValue(serviceProvider, out var scopes)
            ? scopes.Value
            : _scopesByProvider.GetValue(serviceProvider, provider => new Lazy<IObserverDeliveryScope[]>(() => Resolve(provider))).Value;

    static IObserverDeliveryScope[] Resolve(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IEnumerable<IObserverDeliveryScope>)) is not IEnumerable<IObserverDeliveryScope> resolved)
        {
            return _noScopes;
        }

        var array = resolved.ToArray();
        return array.Length == 0 ? _noScopes : array;
    }

    static void Leave(IDisposable[] entered, int count, ObserverDeliveryContext? previous)
    {
        try
        {
            for (var i = count - 1; i >= 0; i--)
            {
                entered[i].Dispose();
            }
        }
        finally
        {
            ObserverDeliveryContext.Current = previous;
        }
    }
}
