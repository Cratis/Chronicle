// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.ExternalServices;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents an implementation of <see cref="ICaptureSourceReader"/> for API sources -
/// polling an HTTP external service for its current items.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for resolving the external service.</param>
/// <param name="httpClientFactory"><see cref="IExternalServiceHttpClientFactory"/> for creating clients for the external service.</param>
[Singleton]
public class ApiCaptureSourceReader(
    IStorage storage,
    IExternalServiceHttpClientFactory httpClientFactory) : ICaptureSourceReader
{
    /// <inheritdoc/>
    public SourceType Type => SourceType.Api;

    /// <inheritdoc/>
    public async Task<IEnumerable<JsonObject>> Read(EventStoreName eventStore, SourceDefinition source)
    {
        var externalServices = await storage.GetEventStore(eventStore).ExternalServices.GetAll();
        var externalService = externalServices.FirstOrDefault(service => service.Name == new ExternalServiceName(source.Api ?? string.Empty))
            ?? throw new MissingExternalServiceForCapture(source.Api ?? string.Empty);

        using var client = await httpClientFactory.Create(externalService);
        var response = await client.GetAsync(source.Route ?? string.Empty);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(content);
        return node switch
        {
            JsonArray array => array.OfType<JsonObject>().ToArray(),
            JsonObject @object => [@object],
            _ => []
        };
    }
}
