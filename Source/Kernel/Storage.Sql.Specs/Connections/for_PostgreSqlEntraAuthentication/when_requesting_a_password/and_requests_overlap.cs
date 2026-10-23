// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication.when_requesting_a_password;

public class and_requests_overlap : given.an_authentication_provider
{
    readonly TaskCompletionSource<AccessToken> _token = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string[] _passwords;

    void Establish() => Credential.Acquire = _ => new(_token.Task);

    async Task Because()
    {
        var first = Authentication.Password().AsTask();
        var second = Authentication.Password().AsTask();
        _token.SetResult(new("shared-token", Now.AddHours(1)));
        _passwords = await Task.WhenAll(first, second);
    }

    [Fact] void should_share_the_token() => _passwords.ShouldEqual(["shared-token", "shared-token"]);
    [Fact] void should_acquire_only_once() => Credential.Requests.Count.ShouldEqual(1);
}
