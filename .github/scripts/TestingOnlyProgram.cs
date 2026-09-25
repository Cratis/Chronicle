// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Testing.Reactors;
using PackageConsumer;

var id = EventSourceId.New();
using var events = new EventScenario();
var result = await events.EventLog.Append(id, new SomethingHappened("hello"));
if (!result.IsSuccess)
{
    Console.Error.WriteLine("EventScenario append failed.");
    return 1;
}

var readModel = new ReadModelScenario<SomethingReadModel>();
await readModel.Given.ForEventSource(id).Events(new SomethingHappened("hello"));
if (readModel.Instance?.What != "hello")
{
    Console.Error.WriteLine("ReadModelScenario did not project the expected value.");
    return 2;
}

var reactor = new ReactorScenario<SomethingReactor>();
await reactor.Given.ForEventSource(id).Events(new SomethingHappened("hello"));
if (SomethingReactor.Handled != "hello")
{
    Console.Error.WriteLine("ReactorScenario did not handle the expected value.");
    return 3;
}

Console.WriteLine("EventScenario append, ReadModelScenario projection and ReactorScenario dispatch passed.");
return 0;

namespace PackageConsumer
{
    /// <summary>
    /// An event used by the package-consumer scenario check.
    /// </summary>
    /// <param name="What">The recorded value.</param>
    [EventType]
    public record SomethingHappened(string What);

    /// <summary>
    /// A read model used by the package-consumer scenario check.
    /// </summary>
    /// <param name="Id">The event source identifier.</param>
    /// <param name="What">The projected value.</param>
    [Passive]
    [FromEvent<SomethingHappened>]
    public record SomethingReadModel(Guid Id, string What);

    /// <summary>
    /// A reactor used by the package-consumer scenario check.
    /// </summary>
    public class SomethingReactor : IReactor
    {
        /// <summary>
        /// The value the reactor observed.
        /// </summary>
        public static string? Handled;

        /// <summary>
        /// Observes an event.
        /// </summary>
        /// <param name="event">The event to observe.</param>
        public void On(SomethingHappened @event) => Handled = @event.What;
    }
}
