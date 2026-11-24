// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing;

public class and_protected_state_gains_a_subject : given.an_unchanged_state_without_a_subject
{
    void Establish() => _schema.Properties["value"].ExtensionData = new Dictionary<string, object?>
    {
        [ComplianceJsonSchemaExtensions.ComplianceKey] = new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
    };

    Task Because() => Reduce();

    [Fact] void should_report_a_modification() => _result.Change.ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_keep_the_subject_lineage() => _result.Change.Subject.ShouldEqual(EventSourceIdValue);
    [Fact] void should_write_the_sink() => _sink.Received(1).ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
}
