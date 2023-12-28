// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents the result of an immediate projection.
/// </summary>
/// <param name="Model">The instance of the Model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ImmediateProjectionResult(object Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount);

/// <summary>
/// Represents the result of an immediate projection.
/// </summary>
/// <typeparam name="T">Type of model.</typeparam>
/// <param name="Model">The instance of the model.</param>
/// <param name="AffectedProperties">Collection of properties that was set.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
public record ImmediateProjectionResult<T>(T Model, IEnumerable<PropertyPath> AffectedProperties, int ProjectedEventsCount);
