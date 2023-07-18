// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;

namespace Aksio.Cratis.Client;

/// <summary>
/// Represents an implementation of <see cref="ICratisClient"/>.
/// </summary>
public class CratisClient : ICratisClient
{
    readonly IClient _client;

    public CratisClient(IClient client)
    {
        _client = client;
    }

    /// <inheritdoc/>
    public Task Connect()
    {
        return _client.Connect();
    }
}
