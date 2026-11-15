// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

public static class a_connection_string_corpus
{
    public const string Username = "synthetic-user-marker";
    public const string Password = "synthetic-password-marker";
    public const string ApiKey = "synthetic-api-marker";
    public const string CertificatePath = "synthetic-certificate-marker.pfx";
    public const string CertificatePassword = "synthetic-certificate-password-marker";
    public const string NumericPassword = "1753198871";
    public const string Tail = "synthetic-tail-marker@server:35000";

    public static string Raw => $"chronicle://{Username}:{Password}@server:35000?apiKey={ApiKey}&skipTlsValidation=false&disableTls=true&skipCompatibilityCheck=true&skipKeepAlive=true&certificatePath={CertificatePath}&certificatePassword={CertificatePassword}";

    public static IEnumerable<string> AllowedAtSignKeys => ["apiKey", "certificatePath", "certificatePassword", "loadBalancer", "srvNameServer"];

    public static string KeywordPassword(string key) => $"chronicle://{Username}:{NumericPassword}?{key}={Tail}";

    public static IEnumerable<string> UncertainInputs => AllowedAtSignKeys.Select(KeywordPassword).Concat(
    [
        KeywordPassword("CeRtIfIcAtEpAtH"),
        $"chronicle://{Username}:{NumericPassword}?certificate%50ath={Tail}",
        $"chronicle://{Username}:{Password}@server:35000/synthetic-path-marker",
        "chronicle://server:35000?synthetic-query-key-marker=synthetic-query-value-marker",
        $"chronicle://server:35000?certificatePath={CertificatePath}",
        $"chronicle://{Username}:{Password}@server:35000/path#synthetic-fragment-marker",
        $"chronicle://{Username}:synthetic%3Fpassword%40marker@server:35000?neutral=value",
        $"chronicle://{Username}:synthetic@password@server:35000?neutral=value",
        "chronicle://[::1]:35000?neutral=synthetic-query-value-marker",
        "chronicle://server:35000,other:35001?neutral=synthetic-query-value-marker",
        $"Host=server;Port=35000;Username={Username};Password={Password};certificatePath={CertificatePath}",
        $"chronicle://server:35000?Host={Username}&Port={NumericPassword}"
    ]);
}
