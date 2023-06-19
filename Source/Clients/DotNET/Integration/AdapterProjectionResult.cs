// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents the result of an <see cref="IAdapterProjectionFor{T}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model the result is for.</typeparam>
/// <param name="Model">The Json representation of the model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record AdapterProjectionResult<TModel>(TModel Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount);
