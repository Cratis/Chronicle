// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Requires the capability to manage the hosted federation this kernel is the authority for, independently of the requested resource.
/// </summary>
public class FederationManagementRequirement : IResourceFreeRequirement;
