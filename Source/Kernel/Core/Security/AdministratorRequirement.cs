// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Requires current administrator authority independently of the requested resource.
/// </summary>
public class AdministratorRequirement : IResourceFreeRequirement;
