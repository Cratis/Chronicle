// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

public class when_starting_a_worker_that_completes : given.a_worker
{
    MyWorkerRequest request;

    void Establish() => request = new("Hello request");

    async Task Because() => await worker.Start(request);

    [Fact] void should_set_correct_execution_context() => worker.ExecutionContext.ShouldEqual(execution_context);
    [Fact] void should_be_handed_the_correct_request() => worker.RequestHandedToPerformWork.ShouldEqual(request);
    [Fact] async Task should_have_completed_status() => (await worker.GetStatus()).ShouldEqual(WorkerStatus.Completed);
    [Fact] async Task should_not_have_any_exception() => (await worker.GetException()).ShouldBeNull();
    [Fact] async Task should_have_expected_result() => (await worker.GetResult()).ShouldEqual(result);
}
