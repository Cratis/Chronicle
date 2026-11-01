// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans.Hosting.for_ChronicleServerStartupTask.given;

namespace Orleans.Hosting.for_RecoverableStartupWork.given;

public class a_recoverable_startup_work : Specification
{
    private protected RecoverableStartupWork _work;
    private protected RecordingStartupWorkEscalation _escalation;
    private protected StartupWorkItem _item;
    protected double _jitter;

    void Establish()
    {
        _escalation = new RecordingStartupWorkEscalation();
        _item = new("ActivateObserver", "store/namespace/observer/event-log");
        _work = new(new a_virtual_time_provider(), _escalation, Substitute.For<ILogger<ChronicleServerStartupTask>>(), () => _jitter);
    }
}
