// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

[Flags]
public enum ProjectionOperationType
{
    None = 0,
    From = 1,
    Join = 2,
    Remove = 4,
}