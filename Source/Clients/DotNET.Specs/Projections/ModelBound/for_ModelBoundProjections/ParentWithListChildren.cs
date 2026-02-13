// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections;

public record ParentWithListChildren(string Name, [ChildrenFrom<TestEvent>] List<ChildProjection> Children);
