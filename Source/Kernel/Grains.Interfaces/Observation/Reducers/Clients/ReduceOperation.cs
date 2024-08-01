// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents the information needed to reduce events.
/// </summary>
/// <param name="Events">Collection of <see cref="AppendedEvent"/> to reduce from.</param>
/// <param name="InitialState">The initial state to reduce from.</param>
public record ReduceOperation(IEnumerable<AppendedEvent> Events, ExpandoObject? InitialState);
