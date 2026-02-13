// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections;

public class ParentWithChildrenProperty
{
    public string Name { get; set; } = string.Empty;

    [ChildrenFrom<TestEvent>]
    public IEnumerable<ChildProjection> Children { get; set; } = [];
}
