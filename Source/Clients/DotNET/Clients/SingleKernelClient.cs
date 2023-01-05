// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Queries;

namespace Aksio.Cratis.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClient"/> for
/// </summary>
public class SingleKernelClient : IClient
{
    readonly IHttpClientFactory _clientFactory;
    readonly SingleKernelOptions _options;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleKernelClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">An <see cref="IHttpClientFactory"/> to create clients from.</param>
    /// <param name="options">The <see cref="SingleKernelOptions"/> to use for connecting.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    public SingleKernelClient(
        IHttpClientFactory httpClientFactory,
        SingleKernelOptions options,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _clientFactory = httpClientFactory;
        _options = options;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> PerformCommand(string route, object? command = null)
    {
        var client = _clientFactory.CreateClient();
        client.BaseAddress = _options.Endpoint;
        HttpResponseMessage response;

        if (command is not null)
        {
            response = await client.PostAsJsonAsync(route, command, options: _jsonSerializerOptions);
        }
        else
        {
            response = await client.PostAsync(route, null);
        }
        var result = await response.Content.ReadFromJsonAsync<CommandResult>(_jsonSerializerOptions);
        return result!;
    }

    /// <inheritdoc/>
    public Task<QueryResult> PerformQuery(string route, IDictionary<string, string>? queryString = null) => throw new NotImplementedException();
}
