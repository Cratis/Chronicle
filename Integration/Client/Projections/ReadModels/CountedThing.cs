// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.ReadModels;

/// <summary>
/// One thing inside a counting read model's membership set.
/// </summary>
/// <param name="Id">The thing's own identity within the set.</param>
/// <param name="Label">A value set from the creating event's content.</param>
/// <param name="IsOpen">A flag a later event flips to a constant.</param>
public record CountedThing(string Id, string Label, bool IsOpen);
