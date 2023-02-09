// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Configuration.Tenants;
using Aksio.Cratis.Kernel.Grains.Observation;
using Orleans;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Internal class for holding all collection names.
/// </summary>
public static class CollectionNames
{
    /// <summary>
    /// The collection that holds <see cref="ObserverState"/>.
    /// </summary>
    public const string Observers = "observers";

    /// <summary>
    /// The collection that holds <see cref="ReminderEntry"/>.
    /// </summary>
    public const string Reminders = "reminders";

    /// <summary>
    /// The collection that holds <see cref="TenantConfigurationState"/>.
    /// </summary>
    public const string TenantConfiguration = "tenants-configuration";

    /// <summary>
    /// The collection that holds event sequences.
    /// </summary>
    public const string EventSequences = "event-sequences";

    /// <summary>
    /// The collection that holds connected clients state.
    /// </summary>
    internal const string ConnectedClients = "connected-clients";

    /// <summary>
    /// The collection that holds schemas.
    /// </summary>
    public const string Schemas = "schemas";
    
    /// <summary>
    /// The collection that holds schemas.
    /// </summary>
    public const string RecoverFailedPartitions = "recover-failed-partitions";
}
