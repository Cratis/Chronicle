// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition for a collection of property actions to perform for all events in the projection.
/// </summary>
/// <param name="Properties">Properties and expressions for each property.</param>
/// <param name="IncludeChildren">Include event types from child projections.</param>
public record AllDefinition(IDictionary<PropertyPath, string> Properties, bool IncludeChildren);
