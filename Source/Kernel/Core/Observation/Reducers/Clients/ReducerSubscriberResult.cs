// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents the result of a reducer subscriber.
/// </summary>
/// <param name="ObserverResult">The <see cref="ObserverSubscriberResult"/> to use.</param>
/// <param name="ReadModelState">The resulting read model state. Null means it should delete it.</param>
public record ReducerSubscriberResult(ObserverSubscriberResult ObserverResult, ExpandoObject? ReadModelState);
