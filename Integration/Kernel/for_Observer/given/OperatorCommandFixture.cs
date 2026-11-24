// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// Fixture holding the reactor that operator commands are given to.
/// </summary>
public class OperatorCommandFixture : ChronicleInProcessFixture
{
    /// <summary>
    /// Gets the reactor whose behavior the specifications steer.
    /// </summary>
    public QuarantineRecoveryReactor Reactor { get; } = new();
}
