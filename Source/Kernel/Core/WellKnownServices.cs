// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Defines the well-known gRPC service names used with the <see cref="Cratis.Chronicle.Grpc.BelongsToAttribute"/>.
/// </summary>
public static class WellKnownServices
{
    /// <summary>
    /// The name of the Users security service.
    /// </summary>
    public const string Users = "Users";

    /// <summary>
    /// The name of the Applications security service.
    /// </summary>
    public const string Applications = "Applications";

    /// <summary>
    /// The name of the Jobs service.
    /// </summary>
    public const string Jobs = "Jobs";

    /// <summary>
    /// The name of the Observers service.
    /// </summary>
    public const string Observers = "Observers";

    /// <summary>
    /// The name of the Namespaces service.
    /// </summary>
    public const string Namespaces = "Namespaces";

    /// <summary>
    /// The name of the EventStores service.
    /// </summary>
    public const string EventStores = "EventStores";

    /// <summary>
    /// The name of the Identities service.
    /// </summary>
    public const string Identities = "Identities";

    /// <summary>
    /// The name of the Recommendations service.
    /// </summary>
    public const string Recommendations = "Recommendations";

    /// <summary>
    /// The service holding the behavior patterns mined from an event store's history.
    /// </summary>
    public const string Patterns = "Patterns";

    /// <summary>
    /// The name of the EventSeeding service.
    /// </summary>
    public const string EventSeeding = "EventSeeding";

    /// <summary>
    /// The name of the ExternalServices service.
    /// </summary>
    public const string ExternalServices = "ExternalServices";

    /// <summary>
    /// The name of the Captures service.
    /// </summary>
    public const string Captures = "Captures";

    /// <summary>
    /// The name of the EventTypes service.
    /// </summary>
    public const string EventTypes = "EventTypes";

    /// <summary>
    /// The name of the Webhooks service.
    /// </summary>
    public const string Webhooks = "Webhooks";

    /// <summary>
    /// The name of the ProjectionEditor service - the projection-editing surface the Workbench drives.
    /// </summary>
    /// <remarks>
    /// Deliberately distinct from the client-facing <c>IProjections</c> contract, which stays hand-written for the
    /// registration and preview operations the client SDK calls. Only the code-generation operations, which no
    /// client SDK uses, moved onto the generated path.
    /// </remarks>
    public const string ProjectionEditor = "ProjectionEditor";

    /// <summary>
    /// The name of the EventSequences service.
    /// </summary>
    public const string EventSequences = "EventSequences";

    /// <summary>
    /// The name of the service exposing <c>[KeyedBy&lt;TKey&gt;]</c> grain queries for event sequences.
    /// </summary>
    /// <remarks>
    /// Deliberately distinct from <see cref="EventSequences"/> - both live in the same generated
    /// <c>Contracts/EventSequences</c> folder (the grain interface's own C# namespace), and sharing a service name
    /// would generate the same file the hand-written <c>Contracts.EventSequences.IEventSequences</c> contract still
    /// occupies. See PLAN2.md's "near-miss" section.
    /// </remarks>
    public const string EventSequenceQueries = "EventSequenceQueries";
}
