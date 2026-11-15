// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_reporting_erasure_through_an_inherited_classification;

public class and_the_subject_is_retained : given.a_stored_generation_with_inherited_protection
{
    AppendedEvent _stored;
    JsonObject _single;
    JsonObject _batch;
    AppendedEvent _singleEvent;
    AppendedEvent _batchEvent;

    async Task Establish()
    {
        var protectedContent = await _manager.Apply("store", "namespace", _classified, Identifier, _plaintext.DeepClone().AsObject());
        _stored = EventWith(protectedContent);
    }

    async Task Because()
    {
        _singleEvent = await _compliance.ReleaseReportingErasure(_stored, _own);

        // The own generation carries no metadata, so the batch must resolve the effective schema before skipping.
        _batchEvent = (await _compliance.ReleaseReportingErasure([_stored], new Dictionary<EventType, EventTypeSchema>
        {
            [_type] = new(_type, EventTypeOwner.Client, EventTypeSource.Code, _own)
        })).Single();
        _single = _converter.ToJsonObject(_singleEvent.Content, _own);
        _batch = _converter.ToJsonObject(_batchEvent.Content, _own);
    }

    [Fact] void should_release_the_inherited_personal_field_for_a_single_event() => _single["name"]!.GetValue<string>().ShouldEqual(_plaintext["name"]!.GetValue<string>());
    [Fact] void should_release_the_inherited_encrypted_field_for_a_single_event() => _single["secret"]!.GetValue<string>().ShouldEqual(_plaintext["secret"]!.GetValue<string>());
    [Fact] void should_release_the_inherited_personal_field_in_a_batch() => _batch["name"]!.GetValue<string>().ShouldEqual(_plaintext["name"]!.GetValue<string>());
    [Fact] void should_release_the_inherited_encrypted_field_in_a_batch() => _batch["secret"]!.GetValue<string>().ShouldEqual(_plaintext["secret"]!.GetValue<string>());
    [Fact] void should_report_nothing_erased_for_a_single_event() => _singleEvent.ErasedProperties.ShouldBeEmpty();
    [Fact] void should_report_nothing_erased_in_a_batch() => _batchEvent.ErasedProperties.ShouldBeEmpty();
}
