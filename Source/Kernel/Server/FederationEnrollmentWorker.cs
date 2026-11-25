// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Federation;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Keeps hosted federation running in the background: creates the authority and issues the listener certificate when this kernel
/// is the authority, redeems an enrollment code from configuration when this kernel is not yet enrolled, and renews this kernel's
/// certificate before it expires. Every step retries, and none of them ever logs a code, secret or key.
/// </summary>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the authority with.</param>
/// <param name="listenerCertificate">The <see cref="FederationListenerCertificate"/> the listener presents.</param>
/// <param name="enrollment">The <see cref="IFederationPeerEnrollment"/> that enrolls and renews this kernel.</param>
/// <param name="identities">The <see cref="IFederationEnrolledIdentities"/> holding this kernel's identity.</param>
/// <param name="logger">The logger.</param>
internal sealed class FederationEnrollmentWorker(
    IOptions<ChronicleOptions> options,
    IGrainFactory grainFactory,
    FederationListenerCertificate listenerCertificate,
    IFederationPeerEnrollment enrollment,
    IFederationEnrolledIdentities identities,
    ILogger<FederationEnrollmentWorker> logger) : BackgroundService
{
    static readonly TimeSpan _retry = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settled = await RunOnce(stoppingToken);
            try
            {
                await Task.Delay(settled ? options.Value.Federation.Enrollment.RenewalCheckInterval : _retry, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    async Task<bool> RunOnce(CancellationToken cancellationToken)
    {
        var federation = options.Value.Federation;
        var settled = true;

        if (federation.Enabled && federation.Hosted.Enabled)
        {
            try
            {
                await grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key).EnsureAuthority();
                if (FederationListener.IssuesOwnCertificate(options.Value))
                {
                    await listenerCertificate.Refresh();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.AuthorityNotReady(ex.GetType().Name, ex.Message);
                settled = false;
            }
        }

        try
        {
            await RedeemConfiguredCode(cancellationToken);
            await enrollment.RenewIfDue(cancellationToken);
        }
        catch (InvalidFederationEnrollment ex)
        {
            // The authority refused or the code is bad: retrying every few seconds would only hammer it, so wait a full interval.
            logger.EnrollmentNotReady(ex.GetType().Name, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.EnrollmentNotReady(ex.GetType().Name, ex.Message);
            settled = false;
        }

        return settled;
    }

    async Task RedeemConfiguredCode(CancellationToken cancellationToken)
    {
        var path = options.Value.Federation.Enrollment.CodePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path) || await identities.Get() is not null)
        {
            return;
        }

        var code = (await File.ReadAllTextAsync(path, cancellationToken)).Trim();
        if (code.Length > 0)
        {
            await enrollment.Redeem(code, cancellationToken);
            RemoveRedeemedCode(path);
        }
    }

    void RemoveRedeemedCode(string path)
    {
        if (!RedeemedEnrollmentCodeFile.TryRemove(path))
        {
            logger.RedeemedCodeCouldNotBeRemoved();
        }
    }
}
