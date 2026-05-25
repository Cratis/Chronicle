// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.ModelBound;

/// <summary>
/// Attribute used to indicate that an event should be appended when an item is added.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class WhenAddedAttribute : Attribute;
