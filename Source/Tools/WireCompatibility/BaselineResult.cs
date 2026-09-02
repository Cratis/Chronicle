// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// The outcome of checking the current wire contract against one released baseline.
/// </summary>
/// <param name="Version">The released version that was compared against.</param>
/// <param name="Report">What the comparison found.</param>
public record BaselineResult(string Version, WireCompatibilityReport Report);
