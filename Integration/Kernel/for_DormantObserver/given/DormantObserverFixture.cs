// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

public class DormantObserverFixture : ChronicleInProcessFixture
{
    public DormantObserverReactor Reactor { get; } = new();

    public DormantAlphaReactor Alpha { get; } = new();

    public DormantBetaReactor Beta { get; } = new();

    public DormantGammaReactor Gamma { get; } = new();
}
