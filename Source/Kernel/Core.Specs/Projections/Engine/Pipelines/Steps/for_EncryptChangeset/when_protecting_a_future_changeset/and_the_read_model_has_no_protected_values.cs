// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_protecting_a_future_changeset;

public class and_the_read_model_has_no_protected_values : given.a_future_changeset
{
    IJsonSchemaMetadataManager _metadata;
    Change[] _original;

    void Establish()
    {
        _projection.TargetReadModelSchema.Returns(new JsonSchema());
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        _step = new EncryptChangeset(new ReadModelsCompliance(_metadata, new ExpandoObjectConverter(new TypeFormats())), new ObjectComparer(), "test-store", "test-namespace");
        UpdateChild();
        _original = _changeset.Changes.ToArray();
    }

    async Task Because() => await _step.ProtectFutureChangeset(_projection, _context);

    [Fact] void should_not_call_compliance() => _metadata.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_leave_the_changes_untouched() => _changeset.Changes.SequenceEqual(_original, ReferenceEqualityComparer.Instance).ShouldBeTrue();
}
