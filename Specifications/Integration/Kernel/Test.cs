// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel;

[EventType("943e0e8e-f581-4211-b31a-7fa6ce5c8ad0")]
public record MyEvent();

[Observer("6f92d202-af9b-44bf-871d-0f5e0d1e95e7")]
public class MyObserver
{
    public void MyEvent(MyEvent @event)
    {
        Console.WriteLine("Got event");
    }
}


[Collection(GlobalCollection.Name)]
public class Test : IClassFixture<KernelFixture>
{
    readonly GlobalFixture _globalFixture;
    readonly KernelFixture _kernelFixture;

    public Test(
        KernelFixture kernelFixture,
        GlobalFixture globalFixture)
    {
        _globalFixture = globalFixture;
        _kernelFixture = kernelFixture;

        _globalFixture.Changes.Subscribe(_ => Console.WriteLine("Got change"));
        _globalFixture.Changes.Where(_ => _.OperationType == ChangeStreamOperationType.Insert).Subscribe(_ => Console.WriteLine("Got insert"));
    }

    [Fact]
    async Task DoStuff()
    {
        _kernelFixture.ExecutionContextManager.Establish(TenantId.Development, CorrelationId.New(), MicroserviceId.Unspecified);
        await _kernelFixture.EventLog.Append(Guid.NewGuid().ToString(), new MyEvent());
        await Task.Delay(5000);

        Assert.True(true);
    }
}
