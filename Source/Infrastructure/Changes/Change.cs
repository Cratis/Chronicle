// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Defines a change as part of a <see cref="Changeset{TSource, TTarget}"/>.
/// </summary>
/// <param name="State">State after change applied.</param>
public record Change(object State);
