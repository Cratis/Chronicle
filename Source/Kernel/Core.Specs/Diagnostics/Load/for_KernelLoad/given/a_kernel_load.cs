// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad.given;

public class a_kernel_load : Specification
{
    protected ManualTimeProvider _time;
    protected KernelLoad _load;

    protected virtual Overload Limits => new()
    {
        MaxOldestPendingCallAgeSeconds = 15,
        MaxPendingCalls = 3,
        MaxRunningCalls = 3,
        RecoverySeconds = 30
    };

    void Establish()
    {
        _time = new();
        _load = new KernelLoad(Options.Create(new ChronicleOptions { Overload = Limits }), NullLogger<KernelLoad>.Instance, null, _time);
    }
}
