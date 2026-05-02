// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Marks a property or record parameter as the <see cref="Subject"/> of an event — the identity used to
/// key per-subject compliance material such as PII encryption keys.
/// </summary>
/// <remarks>
/// When this attribute is present on a property or record constructor parameter of an event type,
/// Chronicle will automatically derive the subject from that value when the caller does not supply
/// an explicit <see cref="Subject"/> to <c>IEventSequence.Append</c>.
///
/// <code>
/// [EventType]
/// public record ShippingAddressChanged(
///     OrderId Order,
///     [Subject] CustomerId Customer,
///     [PII] string City);
/// </code>
///
/// Appending without an explicit subject automatically uses the <c>Customer</c> property as the
/// subject, so PII fields are encrypted under the customer's key rather than the order's key.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SubjectAttribute : Attribute;

