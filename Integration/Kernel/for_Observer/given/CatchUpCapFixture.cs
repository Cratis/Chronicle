// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

/// <summary>
/// Fixture holding the gate the observers behind a catch-up cap deliver through.
/// </summary>
public class CatchUpCapFixture : ChronicleInProcessFixture
{
    /// <summary>
    /// Gets the gate every reactor delivers through.
    /// </summary>
    public CatchUpGate Gate { get; } = new();
}
