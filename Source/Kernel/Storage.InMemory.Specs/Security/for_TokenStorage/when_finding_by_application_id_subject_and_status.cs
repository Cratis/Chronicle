// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_TokenStorage;

public class when_finding_by_application_id_subject_and_status : given.a_token_storage
{
    IEnumerable<Token> _result;

    async Task Establish()
    {
        await _storage.Create(new Token { Id = "match", ApplicationId = "app", Subject = "user", Status = "valid" });
        await _storage.Create(new Token { Id = "wrong-status", ApplicationId = "app", Subject = "user", Status = "revoked" });
        await _storage.Create(new Token { Id = "wrong-application", ApplicationId = "other", Subject = "user", Status = "valid" });
        await _storage.Create(new Token { Id = "wrong-subject", ApplicationId = "app", Subject = "other", Status = "valid" });
    }

    async Task Because() => _result = await _storage.FindByApplicationIdSubjectAndStatus("app", "user", "valid");

    [Fact] void should_return_only_the_matching_token() => _result.Select(_ => _.Id).ShouldContainOnly(["match"]);
}
