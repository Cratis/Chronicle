// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when password and confirmed password do not match.
/// </summary>
public class PasswordConfirmationMismatch() : Exception("Password and confirmed password do not match");
