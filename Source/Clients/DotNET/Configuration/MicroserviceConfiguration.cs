// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IMicroserviceConfiguration"/>.
/// </summary>
public class MicroserviceConfiguration : IMicroserviceConfiguration
{
    readonly IClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroserviceConfiguration"/> class.
    /// </summary>
    /// <param name="client"><see cref="IClient"/> for connecting to Kernel.</param>
    public MicroserviceConfiguration(IClient client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public async Task<StorageForMicroservice> Storage()
    {
        var route = $"/api/configuration/microservices/{ExecutionContextManager.GlobalMicroserviceId}/storage";
        var result = await _client.PerformQuery<StorageForMicroservice>(route);
        return result.Data;
    }
}
