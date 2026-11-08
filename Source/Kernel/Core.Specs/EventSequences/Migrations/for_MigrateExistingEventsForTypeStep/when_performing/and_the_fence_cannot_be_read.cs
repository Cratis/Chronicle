// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_the_fence_cannot_be_read : given.protected_events
{
    string _original;

    void Establish()
    {
        _original = _converter.ToJsonObject(_stored[1][1], _sourceSchema).ToJsonString();
        _keyAccess.GetErasureForRelease(Store, Namespace, "erased-owner").ThrowsAsync(new InvalidOperationException());
    }

    async Task Because() => await Perform();

    [Fact] void should_report_the_fence_failure()
    {
        _result.TryGetException(out var error).ShouldBeTrue();
        error.InnerException.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    }

    [Fact] void should_not_persist_empty_content() => _converter.ToJsonObject(_stored[1][1], _sourceSchema).ToJsonString().ShouldEqual(_original);
    [Fact] void should_not_produce_generations() => _stored.Values.All(generations => generations.Count == 1).ShouldBeTrue();
    [Fact] void should_not_invoke_migration() => _migrationInputs.ShouldBeEmpty();
}
