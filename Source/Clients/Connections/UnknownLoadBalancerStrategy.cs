// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when a connection string specifies a load balancer strategy that is not known.
/// </summary>
/// <param name="name">The name of the unknown strategy.</param>
public class UnknownLoadBalancerStrategy(string name) : Exception($"The load balancer strategy '{name}' is not known. Known strategies are '{RoundRobinLoadBalancerStrategy.StrategyName}' and '{RandomLoadBalancerStrategy.StrategyName}'");
