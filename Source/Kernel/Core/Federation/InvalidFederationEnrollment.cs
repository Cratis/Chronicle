// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The exception that is thrown when an enrollment, or the configuration it depends on, is invalid.
/// </summary>
/// <param name="reason">Why the enrollment is invalid. Never contains a secret.</param>
public class InvalidFederationEnrollment(string reason) : Exception($"The federation enrollment is invalid: {reason}");
