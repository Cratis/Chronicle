// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_a_key_is_missing_without_erasure : given.a_factory
{
    MissingEncryptionKey _missingKey;
    Exception _error;

    void Establish()
    {
        _missingKey = new("source");
        _metadata.ReleaseWithStatus(Store, Namespace, _schema, Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(Task.FromException<StatusedRelease>(_missingKey));
    }

    async Task Because() => _error = (await Catch.Exception(async () => _result = await Create()))!;

    [Fact] void should_propagate_the_missing_key_failure() => _error.ShouldEqual(_missingKey);
    [Fact] void should_not_return_a_skip_or_claim_result() => _result.ShouldBeNull();
    [Fact] void should_leave_stored_ciphertext_unchanged() => CurrentContent().ShouldEqual(_original);
}
