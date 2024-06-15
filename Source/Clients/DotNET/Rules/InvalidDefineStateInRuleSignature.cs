// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Exception that gets thrown when the signature of `DefineState` on a business rule is wrong.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidDefineStateInRuleSignature"/> class.
/// </remarks>
/// <param name="type">Type that has wrong method signature.</param>
public class InvalidDefineStateInRuleSignature(Type type) : Exception($"Invalid `DefineState` signature for '{type.FullName}'. Expected a public or non public instance method signature 'void DefineState(IProjectionBuilderFor<{type.Name}>)'")
{
}
