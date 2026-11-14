// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_FederationListener;

public class when_only_dialing : Specification
{
    readonly List<(int Port, X509Certificate2? Certificate)> _listeners = [];
    X509Certificate2 _loaded;
    bool _issuesOwn;
    Exception _noCertificateNeeded;
    bool _portCollisionReported;

    void Because()
    {
        // Dial-only: the consumer side is on, the listener is not. No certificate, no port, even if both are left at their defaults or collide.
        var options = new ChronicleOptions
        {
            Port = 35100,
            Federation = new() { Enabled = false, Port = 35100, Dial = new FederationDialOptions { Enabled = true, EventStore = "customer" } }
        };
        _loaded = FederationListener.LoadCertificate(options);
        _issuesOwn = FederationListener.IssuesOwnCertificate(options);
        _noCertificateNeeded = Catch.Exception(() => FederationListener.Configure(options, null, (port, cert) => _listeners.Add((port, cert))));
        _portCollisionReported = options.Validate(new ValidationContext(options)).Any(_ => _.MemberNames.Any(name => name.StartsWith(nameof(ChronicleOptions.Federation), StringComparison.Ordinal)));
    }

    [Fact] void should_not_load_a_listener_certificate() => _loaded.ShouldBeNull();
    [Fact] void should_not_issue_a_listener_certificate() => _issuesOwn.ShouldBeFalse();
    [Fact] void should_not_open_a_listener_or_a_port() => _listeners.ShouldBeEmpty();
    [Fact] void should_not_complain_that_there_is_no_certificate() => _noCertificateNeeded.ShouldBeNull();
    [Fact] void should_not_complain_about_the_federation_port() => _portCollisionReported.ShouldBeFalse();
}
