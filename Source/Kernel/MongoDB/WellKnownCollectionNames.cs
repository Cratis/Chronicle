// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Configuration.Tenants;
using Aksio.Cratis.Kernel.Grains.Observation;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Internal class for holding all collection names.
/// </summary>
public static class WellKnownCollectionNames
{
    /// <summary>
    /// The collection that holds <see cref="Event"/>.
    /// </summary>
    public const string EventLog = "event-log";

    /// <summary>
    /// The collection that holds <see cref="Event"/> for the outbox.
    /// </summary>
    public const string Outbox = "outbox";

    /// <summary>
    /// The collection that holds <see cref="Event"/> for the inbox.
    /// </summary>
    public const string Inbox = "inbox";

    /// <summary>
    /// The collection that holds <see cref="Event"/> for the system.
    /// </summary>
    public const string System = "system";

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
    public const string ConnectedClients = "connected-clients";

    /// <summary>
    /// The collection that holds schemas.
    /// </summary>
    public const string Schemas = "schemas";

    /// <summary>
    /// The collection that holds schemas.
    /// </summary>
    public const string FailedPartitions = "failed-partitions";

    /// <summary>
    /// The collection that holds identities.
    /// </summary>
    public const string Identities = "identities";

    /// <summary>
    /// The collection that holds jobs.
    /// </summary>
    public const string Jobs = "jobs";

    /// <summary>
    /// The collection that holds job steps.
    /// </summary>
    public const string JobSteps = "job-steps";

    /// <summary>
    /// The collection that holds failed job steps.
    /// </summary>
    public const string FailedJobSteps = "failed-job-steps";
}
