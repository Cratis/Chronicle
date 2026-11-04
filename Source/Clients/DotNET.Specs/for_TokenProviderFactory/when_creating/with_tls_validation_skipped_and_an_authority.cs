// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.for_TokenProviderFactory.when_creating;

public class with_tls_validation_skipped_and_an_authority : given.a_factory_context
{
    SocketsHttpHandler _handler;

    void Because()
    {
        var options = Options(WithCredentials, new() { Authority = "https://idp.example.com/realms/chronicle" });
        _result = TokenProviderFactory.Create(options, () => options.ConnectionString.ServerAddress, true, NullLoggerFactory.Instance);
        _handler = (SocketsHttpHandler)((ExternalAuthorityTokenProvider)_result).GetMessageHandler();
    }

    [Fact] void should_not_skip_certificate_validation() => _handler.SslOptions.RemoteCertificateValidationCallback.ShouldBeNull();
    [Fact] void should_not_follow_redirects() => _handler.AllowAutoRedirect.ShouldBeFalse();
}
