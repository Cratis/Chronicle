// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Represents an immutable expected-state proof, compared exactly on all three fields.
/// </summary>
/// <param name="Epoch">The store-level marker epoch.</param>
/// <param name="Phase">The Live or specific run phase.</param>
/// <param name="Revision">The monotonically advancing transition revision.</param>
public sealed record UniqueConstraintWriteAuthority(UniqueConstraintEpoch Epoch, UniqueConstraintWritePhase Phase, UniqueConstraintAuthorityRevision Revision);
