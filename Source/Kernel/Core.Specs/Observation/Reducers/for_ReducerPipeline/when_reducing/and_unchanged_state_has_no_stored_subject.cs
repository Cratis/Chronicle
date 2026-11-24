// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

public class and_unchanged_state_has_no_stored_subject : given.an_unchanged_state_without_a_subject
{
    Task Because() => Reduce();

    [Fact] void should_not_report_a_change() => _result.ShouldBeNull();
    [Fact] void should_not_write_the_sink() => _sink.DidNotReceive().ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
    [Fact] void should_not_add_a_subject_to_the_stored_state() => ((IDictionary<string, object?>)_initial).ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
}
