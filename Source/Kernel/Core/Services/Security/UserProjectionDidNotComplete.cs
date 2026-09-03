// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is reported when the user projection did not process a password change successfully.
/// </summary>
public class UserProjectionDidNotComplete() : Exception("The user projection did not complete the password change.");
