// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.Validation;

/// <summary>
/// Implements the shared registration, request, and connect-time target policy.
/// </summary>
/// <param name="options">The kernel configuration.</param>
/// <param name="environment">The kernel runtime environment.</param>
/// <param name="resolver">The host resolver.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class WebhookTargetPolicy(
    IOptions<ChronicleOptions> options,
    IHostEnvironment environment,
    IWebhookHostResolver resolver,
    ILogger<WebhookTargetPolicy> logger) : IWebhookTargetPolicy
{
    readonly Configuration.Webhooks _options = options.Value.Webhooks;
    readonly bool _development = environment.IsDevelopment();

    /// <inheritdoc/>
    public async Task<WebhookTargetValidation> Evaluate(string url, EventStoreName? eventStore = null, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            return new(false, "Webhook target must be an absolute HTTP or HTTPS URL.");
        }

        var authority = CheckAuthority(target, eventStore);
        if (!authority.IsAllowed) return authority;

        IPAddress[] addresses;
        try
        {
            addresses = await resolver.Resolve(target.DnsSafeHost, cancellationToken);
        }
        catch (System.Net.Sockets.SocketException)
        {
            return Decision(target, "Webhook target host could not be resolved.");
        }

        var resolved = CheckAddresses(target, addresses, eventStore);
        return resolved.Reason.Length > 0 ? resolved : authority;
    }

    /// <inheritdoc/>
    public WebhookTargetValidation CheckAuthority(Uri target, EventStoreName? eventStore)
    {
        if (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps)
        {
            return new(false, "Webhook target must use HTTP or HTTPS.");
        }

        if (IPAddress.TryParse(target.DnsSafeHost, out var address) && WebhookAddressRules.IsMetadata(address) && !IsExplicitlyAllowed(target, eventStore))
        {
            return Decision(target, "Cloud metadata addresses require an explicit host allow-list entry in every validation mode.", alwaysEnforce: true);
        }

        var reasons = new List<string>();
        if (target.Scheme != Uri.UriSchemeHttps && !(_development && (_options.AllowInsecureHttp || target.IsLoopback)))
        {
            reasons.Add("Webhook target must use HTTPS.");
        }

        if (!IsExplicitlyAllowed(target, eventStore) && !IsDevelopmentHost(target))
        {
            reasons.Add("Webhook target host is not in Webhooks:AllowedHosts for this event store.");
        }

        return reasons.Count > 0 ? Decision(target, string.Join(' ', reasons)) : WebhookTargetValidation.Allowed;
    }

    /// <inheritdoc/>
    public WebhookTargetValidation CheckAddresses(Uri target, IEnumerable<IPAddress> addresses, EventStoreName? eventStore)
    {
        var resolved = addresses.ToArray();
        if (resolved.Length == 0) return Decision(target, "Webhook target host resolved to no addresses.");
        var allowed = IsExplicitlyAllowed(target, eventStore);
        if (!allowed && resolved.Any(WebhookAddressRules.IsMetadata))
        {
            return Decision(target, "Cloud metadata addresses require an explicit host allow-list entry in every validation mode.", alwaysEnforce: true);
        }

        if (!allowed && !IsDevelopmentHost(target) && resolved.Any(WebhookAddressRules.IsSpecial))
        {
            return Decision(target, "Webhook target resolves to a private or special address.");
        }

        return WebhookTargetValidation.Allowed;
    }

    static bool Matches(string host, string entry)
    {
        if (!entry.StartsWith("*.", StringComparison.Ordinal)) return host.Equals(entry, StringComparison.OrdinalIgnoreCase);
        var suffix = entry[1..];
        return host.Length > suffix.Length && host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    bool IsExplicitlyAllowed(Uri target, EventStoreName? eventStore)
    {
        var hosts = eventStore is not null && _options.EventStoreAllowedHosts.TryGetValue(eventStore.Value, out var overridden)
            ? overridden
            : _options.AllowedHosts;
        var host = target.IdnHost.TrimEnd('.');
        return hosts.Any(entry => Matches(host, entry.Trim().TrimEnd('.')));
    }

    bool IsDevelopmentHost(Uri target) => _development &&
        (target.IsLoopback || target.IdnHost.Equals("host.docker.internal", StringComparison.OrdinalIgnoreCase));

    WebhookTargetValidation Decision(Uri target, string reason, bool alwaysEnforce = false)
    {
        logger.TargetPolicyViolation(target.IdnHost, reason);
        return new(!alwaysEnforce && _options.Validation == WebhookValidationMode.Warn, reason);
    }
}
