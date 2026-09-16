// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.and_the_older_generation_is_appended.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

[Collection(ChronicleCollection.Name)]
public class and_the_older_generation_is_appended(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_camel_case_client(fixture)
    {
        async Task Because()
        {
            Result = await _store.EventLog.Append("contact", new ContactRecordedV1(new ContactDetailsV1("person@example.com", "work")));
            await ReadStoredGenerations();
        }
    }

    [Fact] void should_register_the_migration() => Context.RegistrationSucceeded.ShouldBeTrue();
    [Fact] void should_succeed() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_one_event() => Context.StoredCount.ShouldEqual(1);
    [Fact] void should_migrate_the_nested_property() => Context.GenerationTwo["contact"]!["email"]!.GetValue<string>().ShouldEqual("person@example.com");
    [Fact] void should_preserve_the_unmapped_sibling() => Context.GenerationTwo["contact"]!["label"]!.GetValue<string>().ShouldEqual("work");
    [Fact] void should_keep_the_default_value() => Context.GenerationTwo["status"]!.GetValue<string>().ShouldEqual("active");
}
