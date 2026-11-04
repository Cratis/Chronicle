// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The exception that is thrown when the user information in a connection string is not percent-encoded where it needs to be.
/// </summary>
public class InvalidUserInfo() : Exception("The user information in the Chronicle connection string contains '/' or '?' that is not percent-encoded. Percent-encode the username and password.");
