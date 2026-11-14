// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Federation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Maps the enrollment routes of hosted federation, served on the federation listener.
/// </summary>
/// <remarks>
/// Redeeming an enrollment and fetching the public authority certificate are the only pre-authentication surface of the
/// federation listener, so they are kept minimal: a small request size limit, a rate limit per source, one generic refusal
/// for an unknown, expired or used enrollment, and a constant-time token comparison in the authority. Renewal requires the
/// certificate to renew, which the authority checks against its registry of issued certificates.
/// </remarks>
internal static class FederationEnrollmentEndpoints
{
    /// <summary>
    /// Map the enrollment routes.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to map on.</param>
    internal static void MapFederationEnrollment(this IEndpointRouteBuilder app)
    {
        app.MapGet(FederationEnrollmentRoutes.Authority, Authority).AllowAnonymous();
        app.MapPost(FederationEnrollmentRoutes.Redeem, Redeem).AllowAnonymous();
        app.MapPost(FederationEnrollmentRoutes.Renew, Renew).AllowAnonymous();
    }

    static async Task<IResult> Authority(HttpContext context, IOptions<ChronicleOptions> options, FederationEnrollmentRateLimiter limiter, IGrainFactory grainFactory, IFederationAuthorityKeys keys)
    {
        var (refusal, lease) = Admit(context, options.Value, limiter, FederationEnrollmentBudget.Discovery);
        if (refusal is not null)
        {
            return refusal;
        }

        using var held = lease;
        await grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key).EnsureAuthority();
        var (authority, _) = await keys.GetOrCreate();
        return Results.Bytes(authority.RawData, "application/pkix-cert");
    }

    static async Task<IResult> Redeem(HttpContext context, IOptions<ChronicleOptions> options, FederationEnrollmentRateLimiter limiter, IGrainFactory grainFactory)
    {
        var (refusal, lease) = Admit(context, options.Value, limiter, FederationEnrollmentBudget.Discovery);
        if (refusal is not null)
        {
            return refusal;
        }

        using var held = lease;
        var request = await ReadBody<FederationRedeemRequest>(context);
        if (request is null || !TryDecode(request.Csr, out var csr))
        {
            return Results.BadRequest();
        }

        var issuance = await grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key).Redeem(request.EnrollmentId, request.Secret, csr);
        return ToResult(issuance);
    }

    static async Task<IResult> Renew(HttpContext context, IOptions<ChronicleOptions> options, FederationEnrollmentRateLimiter limiter, IGrainFactory grainFactory)
    {
        var (refusal, lease) = Admit(context, options.Value, limiter, FederationEnrollmentBudget.Renewal);
        if (refusal is not null)
        {
            return refusal;
        }

        using var held = lease;
        var presented = context.Connection.ClientCertificate;
        if (presented is null)
        {
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        var request = await ReadBody<FederationRenewRequest>(context);
        if (request is null || !TryDecode(request.Csr, out var csr))
        {
            return Results.BadRequest();
        }

        var issuance = await grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key).Renew(presented.RawData, csr);
        return ToResult(issuance);
    }

    static (IResult? Refusal, IDisposable? Lease) Admit(HttpContext context, ChronicleOptions options, FederationEnrollmentRateLimiter limiter, FederationEnrollmentBudget budget)
    {
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = FederationEnrollmentRoutes.MaxRequestBodySize;
        }

        if (!limiter.TryAcquire(context.Connection.RemoteIpAddress, options.Federation.Hosted.RequestsPerMinutePerSource, budget))
        {
            return (Results.StatusCode(StatusCodes.Status429TooManyRequests), null);
        }

        var lease = limiter.TryEnter(options.Federation.Hosted.MaxConcurrentEnrollmentRequests);
        return lease is null
            ? (Results.StatusCode(StatusCodes.Status503ServiceUnavailable), null)
            : (null, lease);
    }

    static async Task<TRequest?> ReadBody<TRequest>(HttpContext context)
        where TRequest : class
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<TRequest>(context.RequestAborted);
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or BadHttpRequestException or InvalidOperationException)
        {
            return null;
        }
    }

    static bool TryDecode(string? value, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrEmpty(value) || value.Length > FederationEnrollmentRoutes.MaxRequestBodySize)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static IResult ToResult(FederationIssuance issuance) => issuance.Outcome switch
    {
        FederationIssuanceOutcome.Issued => Results.Json(issuance),
        FederationIssuanceOutcome.InvalidRequest => Results.Json(FederationIssuance.InvalidRequest, statusCode: StatusCodes.Status400BadRequest),
        _ => Results.Json(FederationIssuance.Rejected, statusCode: StatusCodes.Status403Forbidden)
    };
}
