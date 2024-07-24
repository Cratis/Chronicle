// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents the result of an projection.
/// </summary>
/// <param name="Model">The instance of the Model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ProjectionResult(object Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount);

/// <summary>
/// Represents the result of an projection.
/// </summary>
/// <typeparam name="T">Type of model.</typeparam>
/// <param name="Model">The instance of the model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ProjectionResult<T>(T Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount);
