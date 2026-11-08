// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.given;

public class a_policy_matrix : Specification
{
    protected static readonly string[] Targets =
    [
        "chronicle://localhost:35000",
        "chronicle://127.0.0.2:35000",
        "chronicle://[::1]:35000",
        "chronicle://server.example:35000",
        "chronicle+srv://server.example",
        "chronicle://localhost:35000,server.example:35000"
    ];

    protected (Tls Tls, ChronicleConnectionString ConnectionString)[][] _inputs;
    protected bool[][] _results;

    protected void Configure((bool? Options, bool? ConnectionString)[] settings, bool disableTls = false) =>
        _inputs = Targets.Select(target => settings.Select(setting =>
        {
            var tls = new Tls();
            if (setting.Options.HasValue) tls.SkipCertificateValidation = setting.Options.Value;
            var builder = new ChronicleConnectionStringBuilder(target);
            if (setting.ConnectionString.HasValue) builder.SkipTlsValidation = setting.ConnectionString.Value;
            builder.DisableTls = disableTls;
            return (tls, new ChronicleConnectionString(builder.Build()));
        }).ToArray()).ToArray();
}
