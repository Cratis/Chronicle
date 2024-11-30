// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Workers.for_CpuBoundWorker.given;

public class all_dependencies : Specification
{
    protected TestKitSilo silo = new();

    void Establish()
    {
    }
}
