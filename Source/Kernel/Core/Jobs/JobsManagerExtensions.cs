// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension methods for <see cref="IGrainFactory"/> for getting the jobs manager grain.
/// </summary>
public static class JobsManagerExtensions
{
    /// <summary>
    /// Gets the <see cref="IJobsManager"/> grain for a specific event store and namespace.
    /// </summary>
    /// <param name="factory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The event store namespace name.</param>
    /// <returns>The <see cref="IJobsManager"/> grain.</returns>
    public static IJobsManager GetJobsManager(this IGrainFactory factory, EventStoreName eventStoreName, EventStoreNamespaceName namespaceName) =>
        Cratis.Orleans.Jobs.JobsManagerExtensions.GetJobsManager(factory, eventStoreName.Value, namespaceName.Value);
}
