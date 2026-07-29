// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// The exception that is thrown when a property in the JSON being handled has no matching property in the schema
/// describing it.
/// </summary>
/// <remarks>
/// Compliance handling walks the JSON document and looks every property up in the schema to find the compliance
/// metadata that applies to it. A property with no schema counterpart means the document and the schema have
/// drifted apart — typically a stored event whose event type gained or renamed a property without a migration.
/// Without this exception the mismatch surfaced as a bare LINQ error naming neither the property nor the subject.
/// </remarks>
/// <param name="action">The action being performed — <c>apply</c> or <c>release</c>.</param>
/// <param name="propertyPath">The path of the property that has no schema counterpart.</param>
/// <param name="identifier">The compliance subject the value was being handled under.</param>
/// <param name="knownPropertyNames">The property names the schema actually declares.</param>
public class CompliancePropertyNotFoundInSchema(string action, string propertyPath, string identifier, IEnumerable<string> knownPropertyNames)
    : Exception(BuildMessage(action, propertyPath, identifier, knownPropertyNames))
{
    static string BuildMessage(string action, string propertyPath, string identifier, IEnumerable<string> knownPropertyNames) =>
        $"Could not {action} compliance metadata for property '{propertyPath}' of '{identifier}' because the schema does not declare it. The schema declares: {Describe(knownPropertyNames)}.";

    static string Describe(IEnumerable<string> knownPropertyNames)
    {
        var names = knownPropertyNames.ToArray();
        return names.Length == 0 ? "no properties" : string.Join(", ", names.Select(name => $"'{name}'"));
    }
}
