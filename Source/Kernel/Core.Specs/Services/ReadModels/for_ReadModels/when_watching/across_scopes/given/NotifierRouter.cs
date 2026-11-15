// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes.given;

/// <summary>
/// Simulates Orleans routing of <see cref="IProjectionChangesetNotifier"/> by grain key: one real notifier activation
/// per key string, which a spec can replace to simulate losing the activation.
/// </summary>
public class NotifierRouter
{
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly Dictionary<string, ProjectionChangesetNotifier> _activations = [];
    readonly List<string> _requestedKeys = [];
    TaskCompletionSource _subscribedSinceReplace = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets a task that completes once a subscriber registered with an activation created by the latest <see cref="ReplaceAll"/>.
    /// </summary>
    public Task SubscribedSinceReplace => Volatile.Read(ref _subscribedSinceReplace).Task;

    /// <summary>
    /// Gets the distinct keys any producer or watch resolved a notifier for.
    /// </summary>
    public IReadOnlyList<string> RequestedKeys
    {
        get
        {
            lock (_requestedKeys)
            {
                return [.. _requestedKeys];
            }
        }
    }

    /// <summary>
    /// Gets a reference to the notifier for a key; the activation behind it may be replaced.
    /// </summary>
    /// <param name="key">The notifier key.</param>
    /// <returns>A notifier reference for the key.</returns>
    public IProjectionChangesetNotifier For(string key)
    {
        lock (_requestedKeys)
        {
            if (!_requestedKeys.Contains(key))
            {
                _requestedKeys.Add(key);
            }
        }

        return new RoutingNotifier(this, key);
    }

    /// <summary>
    /// Replaces every activation with a fresh one that holds no registrations, as a silo loss would.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReplaceAll()
    {
        await _gate.WaitAsync();
        try
        {
            foreach (var key in _activations.Keys.ToArray())
            {
                _activations[key] = await NewActivation(key);
            }

            Volatile.Write(ref _subscribedSinceReplace, new(TaskCreationOptions.RunContinuationsAsynchronously));
        }
        finally
        {
            _gate.Release();
        }
    }

    static Task<ProjectionChangesetNotifier> NewActivation(string key)
    {
        // Each activation lives in its own silo so a replacement never collides with the activation it replaces.
        var silo = new TestKitSilo();
        silo.AddService(Substitute.For<ILogger<ProjectionChangesetNotifier>>());
        return silo.CreateGrainAsync<ProjectionChangesetNotifier>(key);
    }

    async Task Subscribe(string key, IReadModelChangesetSubscriber subscriber)
    {
        // Read the activation and its generation together so a registration with a replaced activation never counts.
        ProjectionChangesetNotifier activation;
        TaskCompletionSource generation;
        await _gate.WaitAsync();
        try
        {
            activation = await ActivationWithinGate(key);
            generation = _subscribedSinceReplace;
        }
        finally
        {
            _gate.Release();
        }

        await activation.Subscribe(subscriber);
        generation.TrySetResult();
    }

    async Task<ProjectionChangesetNotifier> ActivationWithinGate(string key)
    {
        if (!_activations.TryGetValue(key, out var activation))
        {
            activation = await NewActivation(key);
            _activations[key] = activation;
        }

        return activation;
    }

    async Task<ProjectionChangesetNotifier> Activation(string key)
    {
        await _gate.WaitAsync();
        try
        {
            return await ActivationWithinGate(key);
        }
        finally
        {
            _gate.Release();
        }
    }

    sealed class RoutingNotifier(NotifierRouter router, string key) : IProjectionChangesetNotifier
    {
        public Task Subscribe(IReadModelChangesetSubscriber subscriber) => router.Subscribe(key, subscriber);

        public async Task Unsubscribe(IReadModelChangesetSubscriber subscriber) => await (await router.Activation(key)).Unsubscribe(subscriber);

        public async Task<bool> HasSubscribers() => await (await router.Activation(key)).HasSubscribers();

        public async Task Notify(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change) =>
            await (await router.Activation(key)).Notify(namespaceName, readModelKey, readModel, change);

        public async Task NotifyMany(EventStoreNamespaceName namespaceName, IReadOnlyList<ReadModelChangeNotification> notifications) =>
            await (await router.Activation(key)).NotifyMany(namespaceName, notifications);
    }
}
