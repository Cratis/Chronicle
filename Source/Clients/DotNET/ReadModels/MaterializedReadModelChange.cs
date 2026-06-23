// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents a single change detected when diffing successive materialized read model windows.
/// </summary>
/// <param name="ModelKey">The key of the read model instance that changed.</param>
/// <param name="Instance">The read model instance, or <see langword="null"/> when it was removed.</param>
/// <param name="ChangeType">The <see cref="ReadModelChangeType"/> that occurred.</param>
public record MaterializedReadModelChange(string ModelKey, object? Instance, ReadModelChangeType ChangeType);
