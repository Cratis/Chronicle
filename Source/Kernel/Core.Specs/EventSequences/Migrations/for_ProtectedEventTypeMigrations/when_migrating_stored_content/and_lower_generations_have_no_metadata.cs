// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_stored_content;

public class and_lower_generations_have_no_metadata : given.a_token_protected_only_in_the_latest_generation
{
    async Task Because() => _migrated = await _sut.Migrate(Store, Namespace, _eventType, _protectedContent, _protectedSource, Subject);

    [Fact] void should_not_store_the_token_as_plaintext_in_the_first_generation() => StoredGeneration(1).ShouldNotContain(Token);
    [Fact] void should_not_store_the_token_as_plaintext_in_the_second_generation() => StoredGeneration(2).ShouldNotContain(Token);
    [Fact] void should_keep_the_appended_generation_as_protected() => _migrated[(Concepts.Events.EventTypeGeneration)3].ShouldBeSame(_protectedSource);
}
