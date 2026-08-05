// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event onboarding a supplier; lives on the supplier's own string-org-number event source, so the read
/// model it creates is keyed by a string rather than a <see cref="System.Guid"/>.
/// </summary>
/// <param name="Name">The supplier name.</param>
[EventType]
public record SupplierOnboarded(string Name);
