// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Marks a property or record parameter as the <see cref="Subject"/> used for compliance operations.
/// </summary>
/// <remarks>
/// On an event type, Chronicle derives the append subject from this value when the caller does not supply
/// an explicit <see cref="Subject"/>. On a read model, <see cref="ReadModels.IReadModels.Release{TReadModel}(TReadModel)"/>
/// uses this value to select the encryption key for an instance that needs manual release.
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
/// A read model's attribute does not override the ownership metadata maintained by Chronicle's projection
/// pipeline; managed projection reads use the provenance of each projected value.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SubjectAttribute : Attribute;
