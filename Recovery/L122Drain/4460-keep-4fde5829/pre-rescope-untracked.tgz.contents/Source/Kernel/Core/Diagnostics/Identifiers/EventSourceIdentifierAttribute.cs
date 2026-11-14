// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Compliance.Classification;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Classifies a log parameter that contains an event source identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EventSourceIdentifierAttribute() : DataClassificationAttribute(ChronicleDataClassifications.EventSourceIdentifier);
