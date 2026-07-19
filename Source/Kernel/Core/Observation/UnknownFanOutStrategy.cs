// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when the observers configuration names a fan out strategy that is not known.
/// </summary>
/// <param name="name">The name of the unknown strategy.</param>
public class UnknownFanOutStrategy(string name) : Exception($"The observer fan out strategy '{name}' is not known. Known strategies are '{RoundRobinObserverSubscriberSelector.StrategyName}' and '{RandomObserverSubscriberSelector.StrategyName}'");
