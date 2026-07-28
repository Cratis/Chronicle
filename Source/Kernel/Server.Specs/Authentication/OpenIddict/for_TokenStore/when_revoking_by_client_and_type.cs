// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict.for_TokenStore;

public class when_revoking_by_client_and_type : given.a_token_store
{
    long _count;

    async Task Establish()
    {
        await _storage.Create(new Token { Id = "access", ApplicationId = "app", Type = OpenIddictConstants.TokenTypeHints.AccessToken, Status = OpenIddictConstants.Statuses.Valid });
        await _storage.Create(new Token { Id = "refresh", ApplicationId = "app", Type = OpenIddictConstants.TokenTypeHints.RefreshToken, Status = OpenIddictConstants.Statuses.Valid });
    }

    async Task Because() => _count = await _store.RevokeAsync(null, "app", null, OpenIddictConstants.TokenTypeHints.AccessToken, CancellationToken.None);

    [Fact] void should_report_one_token_revoked() => _count.ShouldEqual(1L);
    [Fact] async Task should_revoke_the_matching_access_token() => (await _storage.GetById("access"))!.Status.ShouldEqual(OpenIddictConstants.Statuses.Revoked);
    [Fact] async Task should_leave_the_token_of_another_type_untouched() => (await _storage.GetById("refresh"))!.Status.ShouldEqual(OpenIddictConstants.Statuses.Valid);
}
