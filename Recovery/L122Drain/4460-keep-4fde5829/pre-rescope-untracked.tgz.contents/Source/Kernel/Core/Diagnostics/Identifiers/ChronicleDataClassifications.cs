// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Compliance.Classification;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Defines data classifications used by Chronicle telemetry.
/// </summary>
public static class ChronicleDataClassifications
{
    /// <summary>
    /// Identifies event source identifiers and keys containing them.
    /// </summary>
    public static readonly DataClassification EventSourceIdentifier = new("Cratis.Chronicle", "EventSourceIdentifier");
}
