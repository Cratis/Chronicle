// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents all the configured microservices.
/// </summary>
[Configuration]
public class Microservices : Dictionary<string, Microservice>
{
    /// <summary>
    /// Get all <see cref="MicroserviceId">microservice ids</see> configured.
    /// </summary>
    /// <returns>Collection of <see cref="MicroserviceId"/>.</returns>
    public IEnumerable<MicroserviceId> GetMicroserviceIds() => Keys.Select(_ => (MicroserviceId)_).ToArray();
}
