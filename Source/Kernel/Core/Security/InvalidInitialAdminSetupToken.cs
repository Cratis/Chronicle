// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception that is thrown when an initial administrator setup token is missing or invalid.
/// </summary>
public class InvalidInitialAdminSetupToken() : Exception("A valid setup token is required to set the initial administrator password.");
