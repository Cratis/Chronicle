// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict.for_TokenStore;

public class when_revoking_by_subject : given.a_token_store
{
    long _count;

    async Task Establish()
    {
        await _storage.Create(new Token { Id = "first", Subject = "user", Status = OpenIddictConstants.Statuses.Valid });
        await _storage.Create(new Token { Id = "second", Subject = "user", Status = OpenIddictConstants.Statuses.Valid });
        await _storage.Create(new Token { Id = "other", Subject = "other-user", Status = OpenIddictConstants.Statuses.Valid });
    }

    async Task Because() => _count = await _store.RevokeBySubjectAsync("user", CancellationToken.None);

    [Fact] void should_report_two_tokens_revoked() => _count.ShouldEqual(2L);
    [Fact] async Task should_revoke_the_first_matching_token() => (await _storage.GetById("first"))!.Status.ShouldEqual(OpenIddictConstants.Statuses.Revoked);
    [Fact] async Task should_revoke_the_second_matching_token() => (await _storage.GetById("second"))!.Status.ShouldEqual(OpenIddictConstants.Statuses.Revoked);
    [Fact] async Task should_leave_the_token_for_another_subject_untouched() => (await _storage.GetById("other"))!.Status.ShouldEqual(OpenIddictConstants.Statuses.Valid);
}
