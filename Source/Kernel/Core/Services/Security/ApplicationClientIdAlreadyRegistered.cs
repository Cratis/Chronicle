// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when an application is added with a client identifier that is already registered.
/// </summary>
/// <param name="clientId">The client identifier that is already in use.</param>
public class ApplicationClientIdAlreadyRegistered(string clientId) : Exception($"An application with client id '{clientId}' is already registered");
