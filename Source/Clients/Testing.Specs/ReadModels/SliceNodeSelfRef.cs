// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

[FromEvent<StateChangeSliceRenamedSelfRef>(key: nameof(StateChangeSliceRenamedSelfRef.SliceId))]
[RemovedWith<StateChangeSliceRemovedSelfRef>(key: nameof(StateChangeSliceRemovedSelfRef.SliceId))]
public record SliceNodeSelfRef([Key] Guid Id, string Name);
