// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception reported when the persisted user credentials did not catch up with their event.
/// The event may still complete later; this failure must not be reported as a successful password change.
/// </summary>
public class UserProjectionDidNotComplete() : Exception("The user projection did not complete the requested change.");
