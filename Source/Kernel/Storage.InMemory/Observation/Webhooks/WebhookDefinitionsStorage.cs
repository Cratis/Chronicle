// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks;

/// <summary>
/// Represents an in-memory implementation of <see cref="IWebhookDefinitionsStorage"/>.
/// </summary>
public sealed class WebhookDefinitionsStorage : IWebhookDefinitionsStorage
{
    readonly ConcurrentDictionary<WebhookId, WebhookDefinition> _definitions = new();
    readonly ReplaySubject<IEnumerable<WebhookDefinition>> _allSubject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDefinitionsStorage"/> class.
    /// </summary>
    public WebhookDefinitionsStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public Task<IEnumerable<WebhookDefinition>> GetAll() => Task.FromResult(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<WebhookDefinition>> ObserveAll() => _allSubject;

    /// <inheritdoc/>
    public Task<bool> Has(WebhookId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<WebhookDefinition> Get(WebhookId id) =>
        Task.FromResult(_definitions[id]);

    /// <inheritdoc/>
    public Task Delete(WebhookId id)
    {
        _definitions.TryRemove(id, out _);
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(WebhookDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    IEnumerable<WebhookDefinition> Snapshot() => _definitions.Values.ToArray();
}
