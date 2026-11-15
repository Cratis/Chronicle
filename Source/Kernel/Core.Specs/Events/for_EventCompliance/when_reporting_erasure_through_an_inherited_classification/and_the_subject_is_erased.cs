// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_reporting_erasure_through_an_inherited_classification;

public class and_the_subject_is_erased : given.a_stored_generation_with_inherited_protection
{
    AppendedEvent _stored;
    AppendedEvent _single;
    AppendedEvent _batch;

    async Task Establish()
    {
        var protectedContent = await _manager.Apply("store", "namespace", _classified, Identifier, _plaintext.DeepClone().AsObject());
        _stored = EventWith(protectedContent);
        await _keys.RecordErasureFor("store", "namespace", Identifier);
        await _keys.DeleteFor("store", "namespace", Identifier);
    }

    async Task Because()
    {
        _single = await _compliance.ReleaseReportingErasure(_stored, _own);

        // The own generation carries no metadata, so the batch must resolve the effective schema before skipping.
        _batch = (await _compliance.ReleaseReportingErasure([_stored], new Dictionary<EventType, EventTypeSchema>
        {
            [_type] = new(_type, EventTypeOwner.Client, EventTypeSource.Code, _own)
        })).Single();
    }

    [Fact] void should_report_the_inherited_personal_field_as_erased_for_a_single_event() => _single.ErasedProperties.ShouldContain("name");
    [Fact] void should_report_the_inherited_personal_field_as_erased_in_a_batch() => _batch.ErasedProperties.ShouldContain("name");
    [Fact] void should_not_release_the_plaintext_for_a_single_event() => Name(_single).ShouldNotEqual(_plaintext["name"]!.GetValue<string>());
    [Fact] void should_not_release_the_plaintext_in_a_batch() => Name(_batch).ShouldNotEqual(_plaintext["name"]!.GetValue<string>());

    string? Name(AppendedEvent @event) => _converter.ToJsonObject(@event.Content, _own)["name"]?.ToString();
}
