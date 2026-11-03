// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_rebuilding;

public class and_the_key_store_is_unavailable : given.a_rebuild_with_erased_subjects
{
    readonly IOException _failure = new("key storage unavailable");
    ReadModelsCompliance _unavailable;
    Exception _error;

    void Establish()
    {
        var keys = Substitute.For<IEncryptionKeyStorage>();
        keys.TryGetLatestFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>())
            .Returns(Task.FromException<EncryptionKeyAtRevision?>(_failure));
        _unavailable = new ReadModelsCompliance(Manager(keys, new Encryption()), _converter);
    }

    async Task Because() => _error = await Catch.Exception(() => Reduce(Live, _unavailable));

    [Fact] void should_report_the_apply_failure() => _error.ShouldBeOfExactType<SchemaMetadataActionFailed>();
    [Fact] void should_preserve_the_infrastructure_error() => _error.InnerException.ShouldBeSame(_failure);
    [Fact] void should_identify_the_apply_context() => _error.Message.ShouldContain("Failed to apply schema metadata");
    [Fact] void should_not_write_a_partial_or_plaintext_result() => _destination.Writes.ShouldEqual(0);
    [Fact] void should_leave_the_destination_empty() => _destination.Documents.ShouldBeEmpty();
}
