// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The direction a hosted federation policy entry governs, seen from the kernel the policy is set on, the acceptor.
/// </summary>
public enum FederationDirection
{
    /// <summary>
    /// The acceptor is the producer: the dialer pulls events from the acceptor's outbox.
    /// </summary>
    AcceptorOutbound = 0,

    /// <summary>
    /// The acceptor is the consumer: the dialer serves events from its own outbox to the acceptor.
    /// </summary>
    AcceptorInbound = 1
}
