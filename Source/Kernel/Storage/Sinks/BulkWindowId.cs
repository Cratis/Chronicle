// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Identifies one uninterrupted, process-local bulk window.
/// </summary>
/// <param name="Value">The unique window identity.</param>
public record BulkWindowId(Guid Value) : ConceptAs<Guid>(Value);
