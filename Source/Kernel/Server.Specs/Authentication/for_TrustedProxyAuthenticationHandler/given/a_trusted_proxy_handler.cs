// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Encodings.Web;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler.given;

public class a_trusted_proxy_handler : Specification
{
    protected const string ProxyAddress = "10.0.0.5";
    protected TrustedProxyAuthentication _proxy;
    protected DefaultHttpContext _context;
    protected AuthenticateResult _result;

    protected async Task Authenticate()
    {
        var chronicleOptions = new ChronicleOptions
        {
            ForwardedHeaders = new() { KnownNetworks = ["10.0.0.0/24"] },
            Authentication = new() { Http = new() { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = _proxy } }
        };
        var schemeOptions = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        schemeOptions.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
        var handler = new TrustedProxyAuthenticationHandler(
            schemeOptions,
            Options.Create(chronicleOptions),
            new TrustedProxyPeers(chronicleOptions),
            NullLoggerFactory.Instance,
            UrlEncoder.Default);
        await handler.InitializeAsync(new AuthenticationScheme(TrustedProxyAuthenticationHandler.SchemeName, null, typeof(TrustedProxyAuthenticationHandler)), _context);
        _result = await handler.AuthenticateAsync();
    }

    protected void FromPeer(string address) => _context.Features.Set<IImmediatePeerFeature>(new ImmediatePeerFeature(IPAddress.Parse(address)));

    void Establish()
    {
        _proxy = new TrustedProxyAuthentication();
        _context = new DefaultHttpContext();
        _context.Request.Method = HttpMethods.Get;
        _context.Request.Headers["X-Forwarded-User"] = "alice";
        _context.Request.Headers["X-Forwarded-Email"] = "alice@example.com";
        _context.Request.Headers["X-Forwarded-Groups"] = "operators, chronicle-admins";
        FromPeer(ProxyAddress);
    }
}
