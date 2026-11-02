// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_token_endpoint_redirects : given.an_external_authority
{
    Exception _result;

    void Establish() => _handler.TokenStatus = HttpStatusCode.TemporaryRedirect;

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_fail() => _result.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_post_only_once() => _handler.TokenRequests.Count().ShouldEqual(1);
    [Fact] void should_not_send_anything_to_the_redirect_target() => _handler.Requests.Exists(_ => _.Uri.Host == "elsewhere.example.net").ShouldBeFalse();
    [Fact] void should_name_the_endpoint_in_the_error_log() => _tokenLog.Entries.Single(_ => _.Level == LogLevel.Error && _.Exception is not null).Message.Contains(TokenUrl, StringComparison.Ordinal).ShouldBeTrue();
}
