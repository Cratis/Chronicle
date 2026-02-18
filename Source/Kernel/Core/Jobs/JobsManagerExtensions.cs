// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Extension method for <see cref="IJobsManager"/>.
/// </summary>
public static class JobsManagerExtensions
{
    /// <summary>
    /// Gets tje <see cref="IJobsManager"/> grain.
    /// </summary>
    /// <param name="factory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The event store namespace name.</param>
    /// <returns>The <see cref="IJobsManager"/> grain.</returns>
    public static IJobsManager GetJobsManager(this IGrainFactory factory, EventStoreName eventStoreName, EventStoreNamespaceName namespaceName)
        => factory.GetGrain<IJobsManager>(0, new JobsManagerKey(eventStoreName, namespaceName));
}
