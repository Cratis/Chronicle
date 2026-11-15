// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

public class QuarantineRecoveryFixture : ChronicleInProcessFixture
{
    public QuarantineRecoveryReactor Reactor { get; } = new();
}
