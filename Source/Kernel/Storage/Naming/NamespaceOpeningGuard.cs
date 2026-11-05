// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the check that runs the first time the storage of a namespace is opened in a process.
/// </summary>
/// <param name="gate">The <see cref="IResourceNameGate"/>.</param>
/// <param name="eventStoreNames">Reads the names of all existing event stores.</param>
/// <param name="checkTimeout">How long one storage check may take before the open is refused; defaults to <see cref="DefaultCheckTimeout"/>.</param>
public sealed class NamespaceOpeningGuard(IResourceNameGate gate, Func<Task<IEnumerable<string>>> eventStoreNames, TimeSpan? checkTimeout = null)
{
    /// <summary>
    /// The default bound on one storage check, kept below the default grain call timeout so a stuck check refuses the
    /// open before the activation that is waiting for it times out.
    /// </summary>
    public static readonly TimeSpan DefaultCheckTimeout = TimeSpan.FromSeconds(20);

    readonly TimeSpan _checkTimeout = checkTimeout ?? DefaultCheckTimeout;

    readonly ConcurrentDictionary<(string EventStore, string Namespace), byte> _checked = new();

    /// <summary>
    /// Check the cheap conditions on every open: quarantine, and the rule unless the namespace is registered.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="namespaces">The <see cref="INamespaceStorage"/> of the event store.</param>
    public void EnsureCanOpen(EventStoreName eventStore, EventStoreNamespaceName @namespace, INamespaceStorage namespaces)
    {
        try
        {
            gate.EnsureCanOpen(eventStore, @namespace);
        }
        catch (InvalidResourceName) when (IsRegistered(namespaces, @namespace))
        {
            gate.MarkRegistered(eventStore, @namespace);
            gate.EnsureAvailable(eventStore, @namespace);
        }
    }

    /// <summary>
    /// Check a namespace that is opened for the first time in this process, creating it when it is not registered yet.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="namespaces">The <see cref="INamespaceStorage"/> of the event store.</param>
    public void EnsureCanCreateIfNew(EventStoreName eventStore, EventStoreNamespaceName @namespace, INamespaceStorage namespaces)
    {
        if (_checked.ContainsKey((eventStore.Value, @namespace.Value)))
        {
            return;
        }

        var existing = Wait(namespaces.GetAll).Select(_ => _.Name.Value).ToArray();
        if (existing.Contains(@namespace.Value))
        {
            gate.MarkRegistered(eventStore, @namespace);
            _checked.TryAdd((eventStore.Value, @namespace.Value), 0);
            return;
        }

        var creation = new ResourceCreation(
            eventStore,
            @namespace,
            existing,
            Wait(eventStoreNames));
        Wait(() => gate.EnsureCanCreate(creation));
        _checked.TryAdd((eventStore.Value, @namespace.Value), 0);
    }

    bool IsRegistered(INamespaceStorage namespaces, EventStoreNamespaceName @namespace) =>
        Wait(namespaces.GetAll).Any(_ => _.Name.Value == @namespace.Value);

    /// <summary>
    /// Runs asynchronous storage work to completion for the synchronous open.
    /// </summary>
    /// <remarks>
    /// Opening a namespace is synchronous, and it is reached from grain activations whose scheduler runs one thing at a
    /// time. The work is therefore started on the thread pool, not merely awaited there: started on the caller's
    /// scheduler, its continuations would be queued behind the very call that blocks waiting for them. Failures still
    /// surface to the caller unchanged, and a check that does not complete within the bound throws
    /// <see cref="TimeoutException"/>, so a check that cannot complete refuses the open rather than hanging it.
    /// </remarks>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="work">Starts the work.</param>
    /// <returns>The result.</returns>
    T Wait<T>(Func<Task<T>> work) => Task.Run(work).WaitAsync(_checkTimeout).GetAwaiter().GetResult();

    void Wait(Func<Task> work) => Task.Run(work).WaitAsync(_checkTimeout).GetAwaiter().GetResult();
}
