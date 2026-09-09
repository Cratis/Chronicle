// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// The exception thrown when an authenticated actor has no meaningful subject.
/// </summary>
public class AuthenticatedUserHasNoSubject() : Exception("The authenticated principal has no stable subject.");
