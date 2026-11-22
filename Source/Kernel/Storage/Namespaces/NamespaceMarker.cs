// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// The singleton namespace-local append fence, stored alongside the data it protects.
/// </summary>
/// <param name="Incarnation">The namespace lifetime owning this marker.</param>
/// <param name="IsOpen">Whether this incarnation admits appends.</param>
/// <param name="Touch">A counter updated by each append transaction so close and append conflict.</param>
public record NamespaceMarker(NamespaceIncarnationId Incarnation, bool IsOpen, long Touch);
