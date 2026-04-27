// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Marks a property or record parameter as the <see cref="Subject"/> of an event — the identity used to
/// key per-subject compliance material such as PII encryption keys.
/// </summary>
/// <remarks>
/// When this attribute is present on a property of an event type, Chronicle will automatically derive
/// the subject from that property value when the caller does not supply an explicit <see cref="Subject"/>
/// to <c>IEventSequence.Append</c>. This keeps event type definitions self-describing and removes the
/// need to thread the subject through every call site.
///
/// <code>
/// [EventType]
/// public record ShippingAddressChanged(
///     OrderId Order,
///     [property: Subject] CustomerId Customer,
///     [property: PII] string City);
/// </code>
///
/// Appending without an explicit subject automatically uses the <c>Customer</c> property as the
/// subject, so PII fields are encrypted under the customer's key rather than the order's key.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SubjectAttribute : Attribute;

