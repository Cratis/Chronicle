// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Attribute used to disable automatic property mapping for a projection model.
/// Can be applied at class level to prevent AutoMap from mapping properties automatically.
/// This attribute is inherited by child projections.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
public sealed class NoAutoMapAttribute : Attribute;
