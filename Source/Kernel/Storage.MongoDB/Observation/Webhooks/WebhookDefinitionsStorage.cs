// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Represents a <see cref="IWebhookDefinitionsStorage"/> for webhook definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class WebhookDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase) : IWebhookDefinitionsStorage
{
    IMongoCollection<WebhookDefinition> Collection => eventStoreDatabase.GetCollection<WebhookDefinition>(WellKnownCollectionNames.WebhookDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<WebhookDefinition>.Empty);
        var definitions = result.ToList();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition>> ObserveAll() =>
        new TransformingSubject<IEnumerable<WebhookDefinition>, IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition>>(
            Collection.Observe(),
            definitions => definitions.Select(definition => definition.ToKernel()));

    /// <inheritdoc/>
    public Task<bool> Has(WebhookId id) =>
        Collection.Find(r => r.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Concepts.Observation.Webhooks.WebhookDefinition> Get(WebhookId id)
    {
        using var result = await Collection.FindAsync(definition => definition.Id == id);
        return result.Single().ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(WebhookId id) =>
        Collection.DeleteOneAsync(definition => definition.Id == id);

    /// <inheritdoc/>
    public Task Save(Concepts.Observation.Webhooks.WebhookDefinition definition) =>
        Collection.ReplaceOneAsync(
            filter: def => def.Id == definition.Identifier,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
