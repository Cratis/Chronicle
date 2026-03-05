// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represent an implementation of <see cref="IWebhookDefinitionComparer"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/>.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class WebhookDefinitionComparer(
    IStorage storage,
    IObjectComparer objectComparer,
    ILogger<WebhookDefinitionComparer> logger) : IWebhookDefinitionComparer
{
    /// <inheritdoc/>
    public async Task<WebhookDefinitionComparisonResult> Compare(
        WebhookKey webhookKey,
        WebhookDefinition first,
        WebhookDefinition second)
    {
        if (!await storage.GetEventStore(webhookKey.EventStore).Webhooks.Has(webhookKey.WebhookId))
        {
            logger.WebhookIsNew(webhookKey.WebhookId);
            return new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null);
        }

        logger.ComparingDefinitions(webhookKey.WebhookId);

        if (objectComparer.Compare(first, second, out _))
        {
            return new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Same, null);
        }

        var changedProperties = new WebhookDefinitionChangedProperties(
            EventTypesChanged: !first.EventTypes.SequenceEqual(second.EventTypes),
            TargetUrlChanged: first.Target.Url != second.Target.Url,
            TargetHeadersChanged: !AreDictionariesEqual(first.Target.Headers, second.Target.Headers),
            AuthorizationChanged: !AreAuthorizationsEqual(first.Target.Authorization, second.Target.Authorization),
            IsReplayableChanged: first.IsReplayable != second.IsReplayable,
            IsActiveChanged: first.IsActive != second.IsActive,
            OwnerChanged: first.Owner != second.Owner,
            EventSequenceIdChanged: first.EventSequenceId != second.EventSequenceId);

        return new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, changedProperties);
    }

    static bool AreDictionariesEqual(IReadOnlyDictionary<string, string> first, IReadOnlyDictionary<string, string> second)
    {
        if (first.Count != second.Count) return false;

        foreach (var kvp in first)
        {
            if (!second.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
            {
                return false;
            }
        }

        return true;
    }

    static bool AreAuthorizationsEqual(WebhookAuthorization first, WebhookAuthorization second)
    {
        if (first.IsT0 && second.IsT0)
        {
            var firstBasic = first.AsT0;
            var secondBasic = second.AsT0;
            return firstBasic.Username == secondBasic.Username && firstBasic.Password == secondBasic.Password;
        }

        if (first.IsT1 && second.IsT1)
        {
            var firstBearer = first.AsT1;
            var secondBearer = second.AsT1;
            return firstBearer.Token == secondBearer.Token;
        }

        if (first.IsT2 && second.IsT2)
        {
            var firstOAuth = first.AsT2;
            var secondOAuth = second.AsT2;
            return firstOAuth.Authority == secondOAuth.Authority &&
                   firstOAuth.ClientId == secondOAuth.ClientId &&
                   firstOAuth.ClientSecret == secondOAuth.ClientSecret;
        }

        return first.IsT3 && second.IsT3;
    }
}
