// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception that is thrown when the initial admin password is set for a user that has already logged in.
/// </summary>
public class InitialAdminPasswordAlreadyUsed() : Exception("Setting the initial admin password is only allowed for users who have not yet logged in");
