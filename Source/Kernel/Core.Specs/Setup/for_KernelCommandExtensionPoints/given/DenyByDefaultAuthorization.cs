// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

/// <summary>
/// Stands in for an application that flips Arc's default and requires an authenticated caller for everything.
/// </summary>
public class DenyByDefaultAuthorization : IAuthorizationAttributeEvaluator
{
    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(Type type) =>
        ApplicationExtensionPoints.Recorder is not null ? (true, null) : null;

    public (bool HasAuthorize, string? Roles)? GetAuthorizationInfo(MethodInfo method) =>
        ApplicationExtensionPoints.Recorder is not null ? (true, null) : null;
}
