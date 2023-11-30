// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel;

public class KernelTest : IClassFixture<KernelFixture>
{
    public KernelTest(KernelFixture fixture)
    {
        KernelFixture = fixture;
        fixture.SetName(GetType().Name);
    }

    protected KernelFixture KernelFixture { get; }
}
