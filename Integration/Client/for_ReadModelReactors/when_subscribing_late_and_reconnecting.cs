// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_subscribing_late_and_reconnecting.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[Collection(ChronicleCollection.Name)]
public class when_subscribing_late_and_reconnecting(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public EventSequenceNumber LateSequence;
        public EventSequenceNumber ReconnectedSequence;

        async Task Because()
        {
            Subscribe();
            await Ready();
            Subscription!.Dispose();
            using var firstTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var first = await EventStore.EventLog.Append("existing", new Reduction(1));
            await ReactorFrames.WaitFor("existing", ReadModelChangeType.Added, first.SequenceNumber, firstTimeout.Token);
            Subscribe();
            await Ready();
            using var lateTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            LateSequence = await Append("existing", new Reduction(2), ReadModelChangeType.Modified, lateTimeout.Token);
            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Connection.Connect();

            // A fresh sentinel must be acknowledged by BOTH consumers after reconnect.
            await Ready();
            using var reconnectedTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            ReconnectedSequence = await Append("existing", new Reduction(3), ReadModelChangeType.Modified, reconnectedTimeout.Token);
        }
    }

    [Fact] void should_never_infer_added_for_the_late_watch() => Context.WatchFrames.For("existing").Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Modified, ReadModelChangeType.Modified]);
    [Fact] void should_keep_both_modification_contexts() => Context.WatchFrames.For("existing").Select(_ => _.Context.SequenceNumber).ShouldEqual([Context.LateSequence, Context.ReconnectedSequence]);
    [Fact] void should_not_infer_an_addition_after_restarting_reactors() => Context.ReactorFrames.For("existing").Select(_ => _.Type).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Modified]);
}
