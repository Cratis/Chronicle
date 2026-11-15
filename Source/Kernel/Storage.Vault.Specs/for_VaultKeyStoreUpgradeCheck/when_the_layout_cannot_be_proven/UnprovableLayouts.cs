// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_the_layout_cannot_be_proven;

/// <summary>
/// Builds clients over which exactly one step of proving the layout fails, in every way a call can fail.
/// </summary>
public static class UnprovableLayouts
{
    /// <summary>
    /// The steps of proving the layout.
    /// </summary>
    public enum Step
    {
        /// <summary>Reading the marker.</summary>
        ReadMarker = 0,

        /// <summary>Listing the root.</summary>
        ListRoot = 1,

        /// <summary>Writing the marker.</summary>
        WriteMarker = 2,

        /// <summary>Reading the marker back after writing it.</summary>
        ReadMarkerBack = 3
    }

    /// <summary>
    /// Gets the failures a call can end in.
    /// </summary>
    public static IEnumerable<(string Name, Func<Exception> Create)> Failures =>
    [
        ("forbidden", () => new VaultApiException(HttpStatusCode.Forbidden, "permission denied")),
        ("server error", () => new VaultApiException(HttpStatusCode.InternalServerError, "internal error")),
        ("unavailable", () => new VaultApiException(HttpStatusCode.ServiceUnavailable, "Vault is sealed")),
        ("timeout", () => new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException())),
        ("network", () => new HttpRequestException("connection refused")),
        ("cancelled", () => new OperationCanceledException()),
        ("unexpected", () => new InvalidOperationException())
    ];

    static VaultApiException NotFound() => new(HttpStatusCode.NotFound, "no value");

    /// <summary>
    /// Creates a client over an empty mount, where one step fails.
    /// </summary>
    /// <param name="step">The <see cref="Step"/> that fails.</param>
    /// <param name="failure">The failure it ends in.</param>
    /// <returns>The client, and the key-value engine it was built over.</returns>
    public static (IVaultClient Client, IKeyValueSecretsEngineV2 Engine) Create(Step step, Exception failure)
    {
        var client = Substitute.For<IVaultClient>();
        var engine = client.V1.Secrets.KeyValue.V2;
        var markerReads = 0;

        engine.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns<Task<Secret<SecretData>>>(call =>
            {
                if (call.ArgAt<string>(0) != VaultPathSegments.LayoutMarker)
                {
                    throw NotFound();
                }

                markerReads++;
                throw (step == Step.ReadMarker && markerReads == 1) || (step == Step.ReadMarkerBack && markerReads == 2) ? failure : NotFound();
            });
        engine.ReadSecretPathsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns<Task<Secret<ListInfo>>>(call => throw (step == Step.ListRoot && call.ArgAt<string>(0).Length == 0 ? failure : NotFound()));
        engine.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns<Task<Secret<CurrentSecretMetadata>>>(call =>
                call.ArgAt<string>(0) == VaultPathSegments.LayoutMarker && step == Step.WriteMarker
                    ? throw failure
                    : Task.FromResult<Secret<CurrentSecretMetadata>>(null!));

        return (client, engine);
    }
}
