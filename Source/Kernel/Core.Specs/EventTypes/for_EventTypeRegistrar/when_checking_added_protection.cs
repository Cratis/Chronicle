// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_checking_added_protection : given.additive_evolution_enabled
{
    const string Protected = """{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":"Sensitive"}]}}}""";
    EventTypeCheckResult _check;

    async Task Because() => _check = (await _subject.Check("test-store", [Registration(Protected)], _storage)).Single();

    [Fact] void should_still_accept_the_registration() => _check.Accepted.ShouldBeTrue();
    [Fact] void should_keep_the_schema_relationship() => _check.Classification.ShouldEqual("Tolerated");
    [Fact] void should_flag_the_protection_addition() => _check.Keyword.ShouldEqual("protection");
    [Fact] void should_identify_the_existing_property() => _check.Path.ShouldContain("name");
    [Fact] void should_warn_about_older_events() => _check.Reason.ShouldContain("not retroactively protected");
    [Fact] void should_not_write_anything() => _eventTypesStorage.ReceivedCalls().ShouldNotContain(_ => _.GetMethodInfo().Name == "TryReplaceSchema" || _.GetMethodInfo().Name == "Register");
}
