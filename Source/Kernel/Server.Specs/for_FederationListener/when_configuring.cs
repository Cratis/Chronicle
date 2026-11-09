// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_FederationListener;

public class when_configuring : Specification
{
    readonly List<(int Port, X509Certificate2 Certificate)> _disabledListeners = [];
    readonly List<(int Port, X509Certificate2 Certificate)> _enabledListeners = [];
    X509Certificate2 _certificate;
    Exception _missingCertificate;
    Exception _collision;
    X509Certificate2 _loadedWhenDisabled;

    void Establish() => _certificate = DevelopmentCertificate.Create();

    void Destroy() => _certificate.Dispose();

    void Because()
    {
        var disabled = new ChronicleOptions();
        _loadedWhenDisabled = FederationListener.LoadCertificate(disabled);
        FederationListener.Configure(disabled, null, (port, cert) => _disabledListeners.Add((port, cert)));

        var enabled = new ChronicleOptions { Federation = new() { Enabled = true, Port = 35100 } };
        FederationListener.Configure(enabled, _certificate, (port, cert) => _enabledListeners.Add((port, cert)));

        _missingCertificate = Catch.Exception(() => FederationListener.LoadCertificate(enabled));
        _collision = Catch.Exception(() => FederationListener.Configure(
            new ChronicleOptions { Port = 35000, Federation = new() { Enabled = true, Port = 35000 } },
            _certificate,
            (_, _) => { }));
    }

    [Fact] void should_not_load_a_certificate_when_disabled() => _loadedWhenDisabled.ShouldBeNull();
    [Fact] void should_not_open_a_listener_when_disabled() => _disabledListeners.ShouldBeEmpty();
    [Fact] void should_open_one_listener_when_enabled() => _enabledListeners.Single().Port.ShouldEqual(35100);
    [Fact] void should_fail_when_enabled_without_a_certificate() => _missingCertificate.Message.ShouldContain("Federation:CertificatePath");
    [Fact] void should_fail_when_the_port_collides() => _collision.Message.ShouldContain("collides");
}
