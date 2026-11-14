// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when a federation policy is invalid or cannot be changed in its current status.
/// </summary>
/// <param name="subject">The subject.</param>
/// <param name="reason">Why the policy is invalid. Never contains a secret.</param>
public class InvalidFederationPolicy(string subject, string reason) : Exception($"The federation policy of '{subject}' is invalid: {reason}");
