// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net;

/// <summary>
/// Represents a <see cref="ILoadBalancerStrategy"/> that is random.
/// </summary>
public class RandomLoadBalancerStrategy : ILoadBalancerStrategy
{
    /// <inheritdoc/>
    public int GetNext(int max) => Random.Shared.Next(max);
}
