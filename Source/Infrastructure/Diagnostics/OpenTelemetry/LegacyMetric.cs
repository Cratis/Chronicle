// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Describes a legacy metric, selected by default until the next major release.
/// </summary>
/// <param name="Name">The legacy instrument name.</param>
/// <param name="Unit">The legacy unit, or null.</param>
/// <param name="Description">The legacy description, or null.</param>
internal record LegacyMetric(string Name, string? Unit, string? Description);
