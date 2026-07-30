// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// The exception that is thrown when an external service is expected to have an HTTP endpoint but does not.
/// </summary>
/// <param name="name">The <see cref="ExternalServiceName"/> of the external service.</param>
public class ExternalServiceIsNotAnHttpService(ExternalServiceName name)
    : Exception($"The external service '{name}' does not have an HTTP endpoint");
