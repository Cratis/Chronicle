// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a <see cref="ReadModelScenario{TReadModel}"/> running in strict event
/// subscription mode is seeded with an event the projection does not subscribe to.
/// </summary>
/// <remarks>
/// By default the scenario mirrors the production projection engine, which filters an event source's stream
/// to the projection's subscribed types — so a seeded event the projection does not handle is silently
/// ignored. Opting in to strict mode via <see cref="ReadModelScenario{TReadModel}.WithStrictEventSubscription"/>
/// turns that silent skip into this loud error, to catch the genuine mistake of seeding the wrong event type.
/// </remarks>
/// <param name="eventTypeName">The identifier of the event type that the projection does not subscribe to.</param>
public class UnsubscribedEventSeeded(string eventTypeName)
    : Exception($"The projection does not subscribe to seeded event '{eventTypeName}'. " +
        "Strict event subscription is enabled — seed only events the projection subscribes to, or remove the " +
        "WithStrictEventSubscription() call to have unsubscribed events silently skipped as the production engine does.");
