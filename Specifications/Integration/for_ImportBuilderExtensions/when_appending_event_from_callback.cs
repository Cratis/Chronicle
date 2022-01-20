// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Integration.for_ImportBuilderExtensions
{
    public class when_appending_event_from_callback : given.no_changes
    {
        const string event_to_append = "Forty Two";

        void Establish() => subject.AppendEvent(_ => event_to_append);

        void Because() => subject.OnNext(new ImportContext<Model, ExternalModel>(changeset, events_to_append));

        [Fact] void should_append_event() => events_to_append.First().ShouldEqual(event_to_append);
    }
}
