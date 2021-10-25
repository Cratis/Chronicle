// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections;

namespace Sample
{
    [Projection("4ae2fd6d-0038-4066-8b6e-423c908deee5")]
    public class DebitAccountProjection : IProjectionFor<DebitAccount>
    {
        public void Define(IProjectionBuilderFor<DebitAccount> builder)
        {
            builder
                .ModelName("my_model")
                .From<DebitAccountOpened>(_ => _
                    .Set(_ => _.Name).To(_ => _.Name)
                    .Set(_ => _.Owner).To(_ => _.Owner))
                .From<DepositToDebitAccountPerformed>(_ => _
                    .Add(_ => _.Balance).With(_ => _.Amount))
                .From<WithdrawalFromDebitAccountPerformed>(_ => _
                    .Subtract(_ => _.Balance).With(_ => _.Amount));
        }
    }

    // public class ProjectionTesting : IPerformBootProcedure
    // {
    //     const string EventTypeA = "d3e83b35-c11e-46cc-91ca-58cd66d1aa9e";
    //     const string EventTypeB = "5addd1d0-c270-4d86-87b5-13b7b97b1dde";
    //     const string EventTypeC = "fe3c32a7-a50d-47ba-bfeb-cf2417453de8";

    //     class TestProvider : IProjectionEventProvider
    //     {
    //         public IObservable<Event> ProvideFor(IProjection projection)
    //         {
    //             var subject = new ReplaySubject<Event>();
    //             subject.OnNext(new Event(0, EventTypeA, DateTimeOffset.UtcNow, "d567f175-f940-4f4d-88ee-d96885a78c1a", new { integer = 42, a_string = "Forty Two" }.AsExpandoObject()));
    //             subject.OnNext(new Event(0, EventTypeB, DateTimeOffset.UtcNow, "d567f175-f940-4f4d-88ee-d96885a78c1a", new { moreStuff = 43 }.AsExpandoObject()));
    //             return subject;
    //         }

    //         public void Pause(IProjection projection) => throw new NotImplementedException();
    //         public void Resume(IProjection projection) => throw new NotImplementedException();
    //         public Task Rewind(IProjection projection) => throw new NotImplementedException();
    //     }

    //     public void Perform()
    //     {
    //         var parser = new JsonProjectionParser();
    //         var projection = parser.Parse(File.ReadAllText("./projection.json"));
    //         var provider = new TestProvider();
    //         var pipeline = new ProjectionPipeline(provider, projection);
    //         var storage = new InMemoryProjectionStorage();
    //         pipeline.StoreIn(storage);
    //         pipeline.Start();
    //     }
    // }
}
