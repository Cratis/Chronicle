// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Configuration;

namespace Aksio.Cratis.Microservices;

/// <summary>
///
/// </summary>
public interface IMicroserviceConfiguration
{
    Task<StorageForMicroservice> Storage();
}


/// <summary>
///
/// </summary>
public class MicroserviceConfiguration : IMicroserviceConfiguration
{
    readonly IClient _client;

    public MicroserviceConfiguration(IClient client)
    {
        _client = client;
    }

    public Task<StorageForMicroservice> Storage() => throw new NotImplementedException();
}
