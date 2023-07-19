// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IMicroserviceConfiguration"/>.
/// </summary>
public class MicroserviceConfiguration : IMicroserviceConfiguration
{
    readonly IConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroserviceConfiguration"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IConnection"/> for connecting to Kernel.</param>
    public MicroserviceConfiguration(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<StorageForMicroservice> Storage()
    {
        var route = $"/api/configuration/microservices/{ExecutionContextManager.GlobalMicroserviceId}/storage";
        var result = await _connection.PerformQuery<StorageForMicroservice>(route);
        return result.Data;
    }
}
