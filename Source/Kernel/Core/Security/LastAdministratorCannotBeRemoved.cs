// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception thrown when removing a privilege would leave no active administrator.
/// </summary>
public class LastAdministratorCannotBeRemoved() : Exception("The last active administrator cannot be removed.");
