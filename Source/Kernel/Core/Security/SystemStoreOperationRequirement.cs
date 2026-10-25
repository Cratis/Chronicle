// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Requires administrator authority only for operations targeting the System store.
/// </summary>
public class SystemStoreOperationRequirement : IAuthorizationRequirement;
