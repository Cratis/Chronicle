// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the automatic mapping settings in effect for a projection level.
/// </summary>
/// <param name="AutoMap">The effective <see cref="AutoMap"/> setting.</param>
/// <param name="NoAutoMapProperties">Read model properties explicitly opted out of AutoMap.</param>
internal sealed record AutoMapSettings(AutoMap AutoMap, IReadOnlySet<string> NoAutoMapProperties);
