// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

/// <summary>
/// Same reducer identity, implementation, and model; only this client's activity declaration differs.
/// </summary>
/// <typeparam name="T">Marker for explicit fixture discovery.</typeparam>
[DependencyInjection.IgnoreConvention]
[Reducer("3418-watched-reducer", isActive: false)]
public class InactiveWatchedReducer<T> : WatchedReducer<T>;
