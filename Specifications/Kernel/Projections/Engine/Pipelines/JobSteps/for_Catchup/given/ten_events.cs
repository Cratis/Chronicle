// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Catchup.given
{
    public class ten_events : a_catchup_step
    {
        protected IEnumerable<Event> events;

        void Establish()
        {
            events = Enumerable
                        .Range(0, 10)
                        .Select(_ => new Event(
                                            (uint)_,
                                            new EventType(Guid.NewGuid(), 1),
                                            DateTimeOffset.UtcNow,
                                            Guid.NewGuid().ToString(),
                                            new ExpandoObject())).ToArray();

            var first_cursor = new Mock<IEventCursor>();
            first_cursor.SetupSequence(_ => _.MoveNext())
                        .ReturnsAsync(true)
                        .ReturnsAsync(false);
            first_cursor.SetupGet(_ => _.Current).Returns(events);
            var second_cursor = new Mock<IEventCursor>();
            second_cursor.Setup(_ => _.MoveNext()).Returns(Task.FromResult(false));

            provider
                .SetupSequence(_ => _.GetFromPosition(projection.Object, IsAny<EventLogSequenceNumber>()))
                .ReturnsAsync(first_cursor.Object)
                .ReturnsAsync(second_cursor.Object);
        }
    }
}
