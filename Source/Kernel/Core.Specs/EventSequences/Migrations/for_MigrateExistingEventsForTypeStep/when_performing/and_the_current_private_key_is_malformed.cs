// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Migrations.for_MigrateExistingEventsForTypeStep.when_performing;

public class and_the_current_private_key_is_malformed : given.protected_events
{
    string _original;

    async Task Establish()
    {
        _original = _converter.ToJsonObject(_stored[1][1], _sourceSchema).ToJsonString();
        await _keys.RecordErasureFor(Store, Namespace, "erased-owner");
        var validKey = (await _keys.TryGetFor(Store, Namespace, "erased-owner"))!;
        _keyAccess.TryGetFor(Store, Namespace, "erased-owner").Returns(new EncryptionKey(validKey.Public, [1, 2, 3]));
    }

    async Task Because() => await Perform();

    [Fact] void should_report_the_key_import_failure() => (_result.TryGetException(out var error) && error.InnerException is CryptographicException).ShouldBeTrue();
    [Fact] void should_not_replace_generation_content() => _sequence.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "ReplaceGenerationContent").ShouldBeFalse();
    [Fact] void should_leave_existing_content_unchanged() => _converter.ToJsonObject(_stored[1][1], _sourceSchema).ToJsonString().ShouldEqual(_original);
    [Fact] void should_not_produce_target_generations() => _stored.Values.All(generations => generations.Count == 1).ShouldBeTrue();
}
