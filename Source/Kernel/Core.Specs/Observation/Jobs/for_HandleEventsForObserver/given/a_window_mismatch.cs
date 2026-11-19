// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_window_mismatch : a_debouncing_job_step
{
    protected static readonly SiloAddress _otherSilo = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 1);
    protected Exception _error;

    protected string Message => _error.Message;

    protected static IReadOnlyDictionary<SiloAddress, Guid[]> Windows(params (SiloAddress Silo, Guid[] Ids)[] windows) =>
        windows.ToDictionary(_ => _.Silo, _ => _.Ids);

    protected async Task Mismatch(IReadOnlyDictionary<SiloAddress, Guid[]> pinned, IReadOnlyDictionary<SiloAddress, Guid[]> confirmed, bool startFirst = true)
    {
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>()).Returns(
            Task.FromResult(pinned),
            Task.FromResult(confirmed));
        if (startFirst)
        {
            await _jobStep.FlushCheckpoint(start: true);
        }

        await _jobStep.ReportNewSuccessfullyHandledEvent(1UL);
        await _jobStep.ReportNewSuccessfullyHandledEvent(2UL);
        _error = await Catch.Exception(() => _jobStep.ReportNewSuccessfullyHandledEvent(3UL));
    }
}
