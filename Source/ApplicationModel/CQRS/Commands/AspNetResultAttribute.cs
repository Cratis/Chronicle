// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Aksio.Cratis.Applications.Commands;

/// <summary>
/// Represents an attribute that indicates that the result of actions in a controller should be returned as an ASP.NET result.
/// </summary>
/// <remarks>
/// Can be used for an entire controller or individual actions.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AspNetResultAttribute : Attribute, IFilterMetadata
{
}
