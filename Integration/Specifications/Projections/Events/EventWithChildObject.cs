// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ReadModels;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Events;

[EventType]
public record EventWithChildObject(ReadModel Child)
{
    public static EventWithChildObject Create() => new(ReadModel.CreateWithRandomValues());
}
