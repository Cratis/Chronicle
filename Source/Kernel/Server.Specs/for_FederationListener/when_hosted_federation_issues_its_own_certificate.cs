// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_FederationListener;

public class when_hosted_federation_issues_its_own_certificate : Specification
{
    readonly List<(int Port, X509Certificate2? Certificate)> _listeners = [];
    ChronicleOptions _hostedWithoutPath;
    X509Certificate2 _loaded;
    bool _issuesItsOwn;
    bool _issuesItsOwnWithAConfiguredPath;
    bool _issuesItsOwnWhenNotHosted;
    Exception _notHostedWithoutCertificate;

    void Establish() => _hostedWithoutPath = new() { Federation = new() { Enabled = true, Port = 35100, Hosted = new() { Enabled = true } } };

    void Because()
    {
        _loaded = FederationListener.LoadCertificate(_hostedWithoutPath);
        FederationListener.Configure(_hostedWithoutPath, _loaded, (port, cert) => _listeners.Add((port, cert)));

        _issuesItsOwn = FederationListener.IssuesOwnCertificate(_hostedWithoutPath);
        _issuesItsOwnWithAConfiguredPath = FederationListener.IssuesOwnCertificate(new() { Federation = new() { Enabled = true, CertificatePath = "listener.pfx", Hosted = new() { Enabled = true } } });
        _issuesItsOwnWhenNotHosted = FederationListener.IssuesOwnCertificate(new() { Federation = new() { Enabled = true } });
        _notHostedWithoutCertificate = Catch.Exception(() => FederationListener.LoadCertificate(new() { Federation = new() { Enabled = true } }));
    }

    [Fact] void should_not_load_a_certificate_from_a_file() => _loaded.ShouldBeNull();
    [Fact] void should_still_open_the_listener() => _listeners.Single().Port.ShouldEqual(35100);
    [Fact] void should_open_the_listener_without_a_fixed_certificate() => _listeners.Single().Certificate.ShouldBeNull();
    [Fact] void should_issue_its_own_certificate() => _issuesItsOwn.ShouldBeTrue();
    [Fact] void should_use_the_configured_certificate_when_there_is_a_path() => _issuesItsOwnWithAConfiguredPath.ShouldBeFalse();
    [Fact] void should_not_issue_its_own_certificate_when_hosted_federation_is_not_enabled() => _issuesItsOwnWhenNotHosted.ShouldBeFalse();
    [Fact] void should_still_require_a_certificate_when_hosted_federation_is_not_enabled() => _notHostedWithoutCertificate.Message.ShouldContain("Federation:CertificatePath");
}
