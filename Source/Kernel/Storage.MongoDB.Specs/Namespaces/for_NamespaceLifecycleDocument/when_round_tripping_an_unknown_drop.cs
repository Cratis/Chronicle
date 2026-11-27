// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleDocument;

public class when_round_tripping_an_unknown_drop : Specification
{
    readonly DateTimeOffset _now = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(3)).AddTicks(7);
    NamespaceLifecycle _original;
    NamespaceLifecycle _copy;
    BsonDocument _document;

    void Establish()
    {
        _original = NamespaceLifecycle.Open("Tenant", NamespaceIncarnationId.New(), _now) with { PreviousIncarnation = NamespaceIncarnationId.New() };
        _original = _original.BeginRemoval(Guid.NewGuid(), _now).BeginAttempt(_now);
        for (var stage = NamespaceRemovalStage.CloseMarker; stage < NamespaceRemovalStage.Drop; stage++)
        {
            _original = _original.CompleteStage(_now).IssueStage();
        }
        _original = _original.RequireRecovery("NamespaceRemovalOutcomeUnknown");
    }

    void Because()
    {
        _document = NamespaceLifecycleDocument.Encode(_original);
        _copy = NamespaceLifecycleDocument.Decode(_document);
    }

    [Fact] void should_preserve_all_recovery_evidence() => _copy.ShouldEqual(_original);
    [Fact] void should_preserve_display_casing() => _copy.Namespace.Value.ShouldEqual("Tenant");
    [Fact] void should_case_fold_only_the_document_key() => _document["_id"].AsString.ShouldEqual("TENANT");
    [Fact] void should_not_truncate_ticks() => _copy.Created.Ticks.ShouldEqual(_now.Ticks);
    [Fact] void should_preserve_the_time_offset() => _copy.Created.Offset.ShouldEqual(_now.Offset);
    [Fact] void should_use_standard_uuid_encoding() => _document["incarnation"].AsBsonBinaryData.SubType.ShouldEqual(BsonBinarySubType.UuidStandard);
}
