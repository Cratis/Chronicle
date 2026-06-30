// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording an amount against a severity, keyed by a <see cref="Severity"/> enum.
/// </summary>
/// <param name="Level">The severity level, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record IncidentRecorded(Severity Level, decimal Amount);
