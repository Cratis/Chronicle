// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Integration.for_ImportBuilderExtensions;

public class when_appending_event_from_callback : given.no_changes
{
    const string event_to_append = "Forty Two";

    void Establish() => subject.AppendEvent(_ => event_to_append);

    void Because() =>
        subject.OnNext(
            new ImportContext<Model, ExternalModel>(
                new AdapterProjectionResult<Model>(new(0, string.Empty, string.Empty), [], 0),
                changeset,
                events_to_append));

    [Fact]
    void should_append_event() => events_to_append.First().ShouldEqual(event_to_append);
}
