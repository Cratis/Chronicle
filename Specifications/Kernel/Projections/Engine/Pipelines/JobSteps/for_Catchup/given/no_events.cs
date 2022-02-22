// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps.for_Catchup.given
{
    public class no_events : a_catchup_step
    {
        void Establish()
        {
            var cursor = new Mock<IEventCursor>();
            cursor.Setup(_ => _.MoveNext()).Returns(Task.FromResult(false));
            provider.Setup(_ => _.GetFromSequenceNumber(projection.Object, IsAny<EventSequenceNumber>())).Returns(Task.FromResult(cursor.Object));
        }
    }
}
