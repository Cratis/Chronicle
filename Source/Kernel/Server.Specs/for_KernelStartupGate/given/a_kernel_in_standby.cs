// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupGate.given;

public class a_kernel_in_standby : a_starting_kernel
{
    void Establish()
    {
        _options.Clustering.SingleWriterLease.Standby = true;
        _lease.IsWaiting.Returns(true);
    }
}
