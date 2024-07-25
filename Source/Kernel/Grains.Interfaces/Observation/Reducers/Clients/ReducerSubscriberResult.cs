// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents the result of a reducer subscriber.
/// </summary>
/// <param name="ObserverResult">The <see cref="ObserverSubscriberResult"/> to use.</param>
/// <param name="ModelState">The resulting model state.</param>
public record ReducerSubscriberResult(ObserverSubscriberResult ObserverResult, ExpandoObject ModelState);
