// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services;

/// <summary>
/// Marks a Core-owned wire model for contract generation even when its existing service is not yet generated.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class WireContractAttribute : Attribute;
