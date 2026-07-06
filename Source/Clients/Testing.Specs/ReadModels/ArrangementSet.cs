// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event that is the authoritative source of a summary's location.
/// </summary>
/// <param name="Location">The location the summary should reflect.</param>
[EventType]
public record ArrangementSet(string Location);
