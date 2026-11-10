// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Namespaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleAspNetCoreStartupDiagnostics;

public class when_the_namespace_comes_from_an_http_header : given.startup_diagnostics
{
    void Establish()
    {
        _marker.MarkCalled();
        _resolver = new HttpHeaderEventStoreNamespaceResolver(
            Substitute.For<IHttpContextAccessor>(),
            Options.Create(new Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions()));
    }

    void Because() => Create().Report(_logger);

    [Fact] void should_warn_naming_the_header() => _logger.Entries.ShouldContain(e => e.Message.Contains("'x-the-header'"));
    [Fact] void should_warn_once() => _logger.Entries.Count.ShouldEqual(1);
}
