// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Describes the link a kernel holds as a dialer of the hosted federation authority it enrolled with.
/// </summary>
/// <remarks>
/// It is a binding like any other, so the keeper, the consumer, the subscriptions, the inbox and the status all apply to it. It differs
/// in what it presents and what it trusts: the certificate obtained by enrollment and the authority root that came with it, instead of a
/// configured certificate and a pinned server key. The authority decides the target, so the binding names none on the authority side.
/// </remarks>
public static class FederationDial
{
    /// <summary>
    /// The id of the binding of the dialer link.
    /// </summary>
    public const string BindingId = "hosted";

    /// <summary>
    /// The client certificate name that stands for the certificate obtained by enrollment.
    /// </summary>
    public const string EnrolledCertificate = "$enrolled";

    /// <summary>
    /// The origin of the binding of the dialer link.
    /// </summary>
    public const string Origin = "Dial";

    /// <summary>
    /// Gets whether a binding is the dialer link.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <returns>True when the binding presents the enrolled certificate.</returns>
    public static bool IsDial(FederationBindingState binding) => binding.ClientCertificate == EnrolledCertificate;

    /// <summary>
    /// Creates the binding of the dialer link from configuration.
    /// </summary>
    /// <param name="dial">The <see cref="FederationDialOptions"/>.</param>
    /// <returns>The binding, or null when dialing is off or no local event store is configured.</returns>
    public static FederationBindingState? Binding(FederationDialOptions dial) =>
        dial.Enabled && !string.IsNullOrWhiteSpace(dial.EventStore)
            ? new(BindingId, dial.LogicalSource, string.Empty, string.Empty, EnrolledCertificate, string.Empty, string.Empty, dial.EventStore, dial.Namespace, Origin)
            : null;
}
