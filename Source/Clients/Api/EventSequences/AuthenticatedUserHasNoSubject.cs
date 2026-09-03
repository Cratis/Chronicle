// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// The exception that is thrown when an authenticated principal has no stable subject claim.
/// </summary>
public class AuthenticatedUserHasNoSubject() : Exception("The authenticated principal has no stable subject claim.");
