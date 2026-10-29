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
[Singleton]
public class WebhookTargetPolicy : IWebhookTargetPolicy
{
    readonly IOutboundTargetSettings _options;
    readonly bool _development;
    readonly IWebhookHostResolver _resolver;
    readonly ILogger<WebhookTargetPolicy> _logger;
    readonly string _targetKind;
    readonly string _configurationSection;
    readonly WebhookManagedIdentity _managedIdentity;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookTargetPolicy"/> class.
    /// </summary>
    /// <param name="options">The kernel configuration.</param>
    /// <param name="environment">The kernel runtime environment.</param>
    /// <param name="resolver">The host resolver.</param>
    /// <param name="logger">The logger.</param>
    public WebhookTargetPolicy(IOptions<ChronicleOptions> options, IHostEnvironment environment, IWebhookHostResolver resolver, ILogger<WebhookTargetPolicy> logger)
        : this(options.Value.Webhooks, ("Webhook", "Webhooks"), resolver, environment, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookTargetPolicy"/> class for an outbound subsystem.
    /// </summary>
    /// <param name="settings">The subsystem's destination settings.</param>
    /// <param name="diagnostics">The target kind and configuration section in diagnostics.</param>
    /// <param name="resolver">The host resolver.</param>
    /// <param name="environment">The kernel runtime environment.</param>
    /// <param name="logger">The logger.</param>
    protected WebhookTargetPolicy(IOutboundTargetSettings settings, (string Kind, string Section) diagnostics, IWebhookHostResolver resolver, IHostEnvironment environment, ILogger<WebhookTargetPolicy> logger)
    {
        _options = settings;
        _development = environment.IsDevelopment();
        _resolver = resolver;
        _logger = logger;
        _targetKind = diagnostics.Kind;
        _configurationSection = diagnostics.Section;
        _managedIdentity = (settings as Configuration.Webhooks)?.ManagedIdentity ?? new();
    }

    /// <inheritdoc/>
    public async Task<WebhookTargetValidation> Evaluate(string url, EventStoreName? eventStore = null, CancellationToken cancellationToken = default)
    {
        var result = await EvaluateTarget(url, eventStore, cancellationToken);
        if (result.Reason.Length > 0)
        {
            var host = Uri.TryCreate(url, UriKind.Absolute, out var target) ? target.IdnHost : string.Empty;
            _logger.TargetPolicyViolation(host, result.Reason);
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<WebhookTargetValidation> EvaluateForDelivery(string url, EventStoreName? eventStore = null, CancellationToken cancellationToken = default) =>
        EvaluateTarget(url, eventStore, cancellationToken);

    /// <inheritdoc/>
    public WebhookTargetValidation CheckManagedIdentity(Concepts.Security.ManagedIdentityAuthorization authorization, string url, EventStoreName? eventStore = null)
    {
        var identity = _managedIdentity;
        if (string.IsNullOrWhiteSpace(authorization.Scope.Value) || !identity.AllowedScopes.Contains(authorization.Scope.Value, StringComparer.OrdinalIgnoreCase))
        {
            return new(false, "Managed identity scope is not in Webhooks:ManagedIdentity:AllowedScopes.");
        }

        var clientAllowed = string.IsNullOrEmpty(authorization.ClientId.Value)
            ? identity.AllowSystemAssigned
            : identity.AllowedClientIds.Contains(authorization.ClientId.Value, StringComparer.Ordinal);
        if (!clientAllowed)
        {
            return new(false, "Managed identity client id is not approved by Webhooks:ManagedIdentity.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var target) || !IsExplicitlyAllowed(target, eventStore))
        {
            return new(false, "Managed identity webhook target host must be in the host allow-list in every validation mode.");
        }

        return WebhookTargetValidation.Allowed;
    }

    /// <inheritdoc/>
    public WebhookTargetValidation CheckAuthority(Uri target, EventStoreName? eventStore)
    {
        if (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps)
        {
            return new(false, $"{_targetKind} target must use HTTP or HTTPS.");
        }

        if (IPAddress.TryParse(target.DnsSafeHost, out var address) && WebhookAddressRules.IsMetadata(address) && !IsExplicitlyAllowed(target, eventStore))
        {
            return Decision("Cloud metadata addresses require an explicit host allow-list entry in every validation mode.", OutboundTargetViolation.Metadata);
        }

        var reasons = new List<string>();
        if (target.Scheme != Uri.UriSchemeHttps && !(_development && (_options.AllowInsecureHttp || target.IsLoopback)))
        {
            reasons.Add($"{_targetKind} target must use HTTPS.");
        }

        if (!IsExplicitlyAllowed(target, eventStore) && !IsDevelopmentHost(target))
        {
            reasons.Add($"{_targetKind} target host is not in {_configurationSection}:AllowedHosts for this event store.");
        }

        return reasons.Count > 0 ? Decision(string.Join(' ', reasons)) : WebhookTargetValidation.Allowed;
    }

    /// <inheritdoc/>
    public bool RequiresDedicatedConnection(Uri target, EventStoreName? eventStore) => IsExplicitlyAllowed(target, eventStore);

    /// <inheritdoc/>
    public WebhookTargetValidation CheckAddresses(Uri target, IEnumerable<IPAddress> addresses, EventStoreName? eventStore)
    {
        var resolved = addresses.ToArray();
        if (resolved.Length == 0) return Decision($"{_targetKind} target host resolved to no addresses.");
        var allowed = IsExplicitlyAllowed(target, eventStore);
        if (!allowed && resolved.Any(WebhookAddressRules.IsMetadata))
        {
            return Decision("Cloud metadata addresses require an explicit host allow-list entry in every validation mode.", OutboundTargetViolation.Metadata);
        }

        return allowed || IsDevelopmentHost(target) ? WebhookTargetValidation.Allowed : CheckUnapprovedAddresses(resolved);
    }

    static bool Matches(string host, string entry)
    {
        if (!entry.StartsWith("*.", StringComparison.Ordinal)) return host.Equals(entry, StringComparison.OrdinalIgnoreCase);
        var suffix = entry[1..];
        return host.Length > suffix.Length && host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    async Task<WebhookTargetValidation> EvaluateTarget(string url, EventStoreName? eventStore, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            return new(false, $"{_targetKind} target must be an absolute HTTP or HTTPS URL.");
        }

        var authority = CheckAuthority(target, eventStore);
        if (!authority.IsAllowed) return authority;

        IPAddress[] addresses;
        try
        {
            addresses = await _resolver.Resolve(target.DnsSafeHost, cancellationToken);
        }
        catch (System.Net.Sockets.SocketException)
        {
            return Decision($"{_targetKind} target host could not be resolved.");
        }

        var resolved = CheckAddresses(target, addresses, eventStore);
        return resolved.Reason.Length > 0 ? resolved : authority;
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

    WebhookTargetValidation CheckUnapprovedAddresses(IPAddress[] addresses)
    {
        if (addresses.Any(WebhookAddressRules.IsLocal))
        {
            return Decision($"{_targetKind} target resolves to a private or special address.", OutboundTargetViolation.Local);
        }

        if (addresses.Any(WebhookAddressRules.IsSpecial))
        {
            return Decision($"{_targetKind} target resolves to a private or special address.", OutboundTargetViolation.Private);
        }

        return WebhookTargetValidation.Allowed;
    }

    WebhookTargetValidation Decision(string reason, OutboundTargetViolation violation = OutboundTargetViolation.Policy)
    {
        var blocked = _options.Validation switch
        {
            WebhookValidationMode.Warn => violation == OutboundTargetViolation.Metadata,
            WebhookValidationMode.BlockLocalAddresses => violation is OutboundTargetViolation.Metadata or OutboundTargetViolation.Local,
            WebhookValidationMode.BlockPrivateAddresses => violation is not OutboundTargetViolation.Policy,
            WebhookValidationMode.Enforce => true,
            _ => true
        };
        return new(!blocked, reason);
    }
}
