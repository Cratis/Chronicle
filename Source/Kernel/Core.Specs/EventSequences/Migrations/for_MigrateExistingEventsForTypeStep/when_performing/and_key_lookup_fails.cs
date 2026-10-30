// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_key_lookup_fails : given.protected_events
{
    string _original;

    void Establish()
    {
        _original = _converter.ToJsonObject(_stored[1][1], _sourceSchema).ToJsonString();
        _keyAccess.TryGetFor(Store, Namespace, "erased-owner").ThrowsAsync(new MissingEncryptionKey("erased-owner"));
    }

    async Task Because() => await Perform();

    [Fact] void should_report_the_failure() => _result.TryGetException(out _).ShouldBeTrue();
    [Fact] void should_not_replace_generation_content() => _sequence.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "ReplaceGenerationContent").ShouldBeFalse();
    [Fact] void should_leave_existing_content_unchanged() => _converter.ToJsonObject(_stored[1][1], _sourceSchema).ToJsonString().ShouldEqual(_original);
    [Fact] void should_not_produce_target_generations() => _stored.Values.All(generations => generations.Count == 1).ShouldBeTrue();
}
