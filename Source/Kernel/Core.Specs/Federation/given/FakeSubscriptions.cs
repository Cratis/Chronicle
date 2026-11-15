// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake subscription source.
/// </summary>
public class FakeSubscriptions : IFederationSubscriptions
{
    public List<FederationSubscription> Subscriptions { get; } = [];

    public Task<IReadOnlyList<FederationSubscription>> Get(FederationBindingState binding) =>
        Task.FromResult<IReadOnlyList<FederationSubscription>>([.. Subscriptions]);
}
