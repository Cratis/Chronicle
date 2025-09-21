// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB <see cref="WebhookDefinition"/> representations.
/// </summary>
public static class WebhookDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/> to a MongoDB <see cref="WebhookDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel reactor definition.</param>
    /// <returns>The MongoDB reactor definition.</returns>
    public static WebhookDefinition ToMongoDB(this Concepts.Observation.Webhooks.WebhookDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            EventSequenceId = definition.EventSequenceId,
            EventTypes = definition.EventTypes.ToDictionary(
                et => et.EventType.ToString(),
                et => et.Key.ToString()),
            Target = definition.Target.ToMongoDB(),
            IsReplayable = definition.IsReplayable,
            IsActive = definition.IsActive
        };

    /// <summary>
    /// Converts a MongoDB <see cref="WebhookDefinition"/> to a Kernel <see cref="Concepts.Observation.Reactors.ReactorDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB reactor definition.</param>
    /// <returns>The Kernel reactor definition.</returns>
    public static Concepts.Observation.Webhooks.WebhookDefinition ToKernel(this WebhookDefinition definition) =>
        new(
            definition.Id,
            definition.Owner,
            definition.EventSequenceId,
            definition.EventTypes.Select(kvp => new EventTypeWithKeyExpression(EventType.Parse(kvp.Key), (PropertyExpression)kvp.Value)),
            definition.Target.ToKernel(),
            definition.IsReplayable);

    static Concepts.Observation.Webhooks.WebhookTarget ToKernel(this WebhookTarget target) => new(
        target.Url,
        target.Authentication,
        target.Username,
        target.Passsword,
        target.BearerToken,
        target.Headers.AsReadOnly());

    static WebhookTarget ToMongoDB(this Concepts.Observation.Webhooks.WebhookTarget target) => new()
    {
        Url = target.Url,
        Authentication = target.Authentication,
        BearerToken = target.BearerToken,
        Passsword = target.Password,
        Username = target.Username
    };
}
