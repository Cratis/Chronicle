// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines an attribute that marks the member of an edge holding the key of the node it starts at.
/// </summary>
public interface IGraphSourceAttribute : IGraphEndpointAttribute;
