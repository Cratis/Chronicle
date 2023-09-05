// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents the reducer delegate.
/// </summary>
/// <param name="events">Collection of <see cref="AppendedEvent"/> to reduce from.</param>
/// <param name="initialState">The initial state.</param>
/// <returns>The reduced state.</returns>
public delegate Task<ExpandoObject> ReducerDelegate(IEnumerable<AppendedEvent> events, ExpandoObject? initialState);
