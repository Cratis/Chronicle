// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

public class when_starting_a_worker_that_fails : given.a_worker
{
    MyWorkerRequest request;

    public when_starting_a_worker_that_fails() => exception_to_throw = new("Something went wrong");

    void Establish() => request = new("Hello request");

    async Task Because() => await worker.Start(request);

    [Fact] void should_set_correct_execution_context() => worker.ExecutionContext.ShouldEqual(execution_context);
    [Fact] void should_be_handed_the_correct_request() => worker.RequestHandedToPerformWork.ShouldEqual(request);
    [Fact] async Task should_have_failed_status() => (await worker.GetStatus()).ShouldEqual(WorkerStatus.Failed);
    [Fact] async Task should_have_exception_that_was_thrown() => (await worker.GetException()).ShouldEqual(exception_to_throw);
    [Fact] async Task should_not_have_any_result() => (await worker.GetResult()).ShouldBeNull();
}
