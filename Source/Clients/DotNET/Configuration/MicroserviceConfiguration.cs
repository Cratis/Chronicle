// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IMicroserviceConfiguration"/>.
/// </summary>
public class MicroserviceConfiguration : IMicroserviceConfiguration
{
    readonly IClient _client;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicroserviceConfiguration"/> class.
    /// </summary>
    /// <param name="client"><see cref="IClient"/> for connecting to Kernel.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    public MicroserviceConfiguration(
        IClient client,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _client = client;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<StorageForMicroservice> Storage()
    {
        var route = $"/api/configuration/microservices/{ExecutionContextManager.GlobalMicroserviceId}/storage";

        var result = await _client.PerformQuery(route);
        var element = (JsonElement)result.Data;
        return element.Deserialize<StorageForMicroservice>(_jsonSerializerOptions)!;
    }
}
