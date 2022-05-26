// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections.Grains;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapterProjectionFor{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class AdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>
{
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IProjection _projection;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> to work with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
    public AdapterProjectionFor(IProjection projection, JsonSerializerOptions jsonSerializerOptions)
    {
        _projection = projection;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<TModel> GetById(EventSourceId eventSourceId)
    {
        var jsonObject = await _projection.GetModelInstanceById(eventSourceId);
        return jsonObject.Deserialize<TModel>(_jsonSerializerOptions)!;
    }
}
