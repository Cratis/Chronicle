// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

public record EventWithIntProperty
{
    public int SomeProperty { get; init; }
}
