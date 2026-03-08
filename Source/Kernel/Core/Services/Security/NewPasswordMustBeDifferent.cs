// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when the new password must be different from the old password.
/// </summary>
public class NewPasswordMustBeDifferent() : Exception("The new password must be different from the old password");
