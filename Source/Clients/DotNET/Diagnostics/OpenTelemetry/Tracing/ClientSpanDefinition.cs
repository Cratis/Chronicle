// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>The legacy and convention names and kind of one client operation.</summary>
/// <param name="LegacyName">The existing operation name.</param>
/// <param name="ConventionName">The shared convention name.</param>
/// <param name="Kind">The span kind.</param>
internal record ClientSpanDefinition(string LegacyName, string ConventionName, ActivityKind Kind);
