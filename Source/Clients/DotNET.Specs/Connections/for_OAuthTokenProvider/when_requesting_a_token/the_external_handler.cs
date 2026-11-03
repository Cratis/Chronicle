// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class the_external_handler : Specification
{
    System.Net.Http.SocketsHttpHandler _handler;

    void Because() => _handler = OAuthTokenProvider.CreateExternalHttpMessageHandler();

    void Destroy() => _handler.Dispose();

    [Fact] void should_not_follow_redirects() => _handler.AllowAutoRedirect.ShouldBeFalse();
    [Fact] void should_use_the_default_certificate_validation() => _handler.SslOptions.RemoteCertificateValidationCallback.ShouldBeNull();
}
