// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Represents the settings for running the client in serverless mode.
/// </summary>
/// <remarks>
/// Bound from <c language="json">Cratis:Chronicle:Serverless</c>. Receiver runtime settings, such as the timestamp tolerance, the
/// delivery limits and the signing algorithm, are added to this type when the receiver is introduced.
/// </remarks>
[Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
public class ChronicleServerlessOptions
{
    /// <summary>
    /// Gets or sets whether serverless mode is on. Defaults to <see cref="ServerlessMode.Off"/>.
    /// </summary>
    public ServerlessMode Mode { get; set; } = ServerlessMode.Off;

    /// <summary>
    /// Gets or sets the absolute base URL the kernel uses to reach the receiver of HTTP deliveries.
    /// </summary>
    public Uri? ReceiverUrl { get; set; }

    /// <summary>
    /// Gets or sets the path of the receiver endpoint, relative to <see cref="ReceiverUrl"/>.
    /// </summary>
    public string ReceiverPath { get; set; } = "/chronicle/deliveries";

    /// <summary>
    /// Gets or sets when the client registers its artifacts. Defaults to <see cref="ServerlessRegistration.Deploy"/>.
    /// </summary>
    public ServerlessRegistration Registration { get; set; } = ServerlessRegistration.Deploy;

    /// <summary>
    /// Gets or sets the authentication settings for the receiver.
    /// </summary>
    public ServerlessAuthOptions Auth { get; set; } = new();

    /// <summary>
    /// Gets or sets the settings for how the kernel delivers events to the receiver.
    /// </summary>
    public ServerlessDeliveryOptions Delivery { get; set; } = new();

    /// <summary>
    /// Gets or sets the delivery used by observers that declare none. Defaults to <see cref="ObserverDelivery.Http"/>.
    /// </summary>
    /// <remarks>
    /// HTTP delivery is not available in this version of the client yet, so observers that resolve to it fail at
    /// startup. Set this to <see cref="ObserverDelivery.Stream"/> to skip them in serverless mode instead.
    /// </remarks>
    public ObserverDelivery DefaultDelivery { get; set; } = ObserverDelivery.Http;

    /// <summary>
    /// Gets or sets the generation the deployment registered its artifacts under. Zero means it is not set.
    /// </summary>
    /// <remarks>
    /// Required when <see cref="Registration"/> is <see cref="ServerlessRegistration.Deploy"/>, except in Development.
    /// The lazy fingerprint check compares this value with the stored deployment metadata; it does not enforce registration.
    /// </remarks>
    public ulong DeploymentGeneration { get; set; }

    /// <summary>
    /// Gets or sets whether to check deployment definitions in the background. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Only used in serverless <see cref="ServerlessRegistration.Deploy"/> mode. The advisory check runs once per
    /// client and event-store name, across namespaces. Failures and differences are logged without blocking callers
    /// or changing registration readiness. Disable this to avoid the metadata query and local fingerprint computation.
    /// </remarks>
    public bool CheckArtifactFingerprint { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServerlessRegistration.OnColdStart"/> is acknowledged as
    /// intended outside the Development environment.
    /// </summary>
    public bool AllowOnColdStartOutsideDevelopment { get; set; }

    /// <summary>
    /// Gets or sets the per-observer settings, keyed by observer identifier.
    /// </summary>
    /// <remarks>
    /// Observer identifiers that contain a colon cannot be used as configuration keys.
    /// </remarks>
    public IDictionary<string, ServerlessObserverOptions> Observers { get; set; } = new Dictionary<string, ServerlessObserverOptions>(StringComparer.Ordinal);
}
