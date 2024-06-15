// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Exception that gets thrown when there are an invalid number of model keys on a model. Only one is allowed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidNumberOfModelKeys"/> class.
/// </remarks>
/// <param name="type">Type that is invalid.</param>
/// <param name="properties">Properties that has model key.</param>
public class InvalidNumberOfModelKeys(Type type, IEnumerable<string> properties) : Exception($"Invalid number of model keys on '{type.FullName}'. Only one allowed. (Keys = {string.Join(',', properties)}) ")
{
}
