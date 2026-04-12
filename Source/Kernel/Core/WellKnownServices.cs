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
}
