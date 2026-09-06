// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Compares two versions of the Chronicle wire contract and reports every way the newer one fails to serve the older.
/// </summary>
/// <remarks>
/// One engine answers both questions Chronicle asks about compatibility, because they are the same question with
/// different inputs: the release gate asks whether the contract at HEAD still serves the first release of the
/// current major; the kernel asks, on every connect, whether it still serves the client that just arrived. Both
/// reduce to one question - whether everything the older side expects is still there, unchanged, in the newer one.
/// <para>
/// Additions are never reported. A newer contract may add services, methods, messages, fields and enum values
/// freely - an older peer simply does not use them.
/// </para>
/// </remarks>
public static class WireCompatibilityChecker
{
    /// <summary>
    /// Checks whether a contract still serves an older one.
    /// </summary>
    /// <param name="expected">The older contract, as the side that has to keep working sees it.</param>
    /// <param name="actual">The newer contract that has to keep serving it.</param>
    /// <returns>A <see cref="WireCompatibilityReport"/> listing everything that would break.</returns>
    public static WireCompatibilityReport Check(WireContract expected, WireContract actual)
    {
        var incompatibilities = new List<WireIncompatibility>();

        CheckServices(expected, actual, incompatibilities);
        CheckMessages(expected, actual, incompatibilities);
        CheckEnums(expected, actual, incompatibilities);

        return incompatibilities.Count == 0
            ? WireCompatibilityReport.Compatible
            : new WireCompatibilityReport([.. incompatibilities.OrderBy(_ => _.Path, StringComparer.Ordinal).ThenBy(_ => _.Kind)]);
    }

    static void CheckServices(WireContract expected, WireContract actual, List<WireIncompatibility> incompatibilities)
    {
        foreach (var (name, service) in expected.Services)
        {
            if (!actual.Services.TryGetValue(name, out var actualService))
            {
                incompatibilities.Add(new(
                    WireIncompatibilityKind.ServiceRemoved,
                    name,
                    "The service is gone, so every call the older side makes to it fails as unimplemented."));
                continue;
            }

            foreach (var method in service.Methods.Values)
            {
                CheckMethod(name, method, actualService, incompatibilities);
            }
        }
    }

    static void CheckMethod(
        string serviceName,
        WireMethod method,
        WireService actualService,
        List<WireIncompatibility> incompatibilities)
    {
        var path = $"{serviceName}/{method.Name}";

        if (!actualService.Methods.TryGetValue(method.Name, out var actual))
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.MethodRemoved,
                path,
                "The method is gone, so calls to it fail as unimplemented."));
            return;
        }

        if (!string.Equals(method.InputType, actual.InputType, StringComparison.Ordinal) ||
            !string.Equals(method.OutputType, actual.OutputType, StringComparison.Ordinal))
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.MethodSignatureChanged,
                path,
                $"The signature changed from {method.Signature} to {actual.Signature}."));
        }

        // Streaming is part of the HTTP/2 call shape, not the payload: a unary caller against a streaming method
        // hangs or errors rather than decoding anything wrong, so it is called out separately from the types.
        if (method.ClientStreaming != actual.ClientStreaming || method.ServerStreaming != actual.ServerStreaming)
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.MethodStreamingChanged,
                path,
                $"The call went from {Describe(method)} to {Describe(actual)}, which is a different call shape on the wire."));
        }
    }

    static void CheckMessages(WireContract expected, WireContract actual, List<WireIncompatibility> incompatibilities)
    {
        foreach (var (name, message) in expected.Messages)
        {
            if (!actual.Messages.TryGetValue(name, out var actualMessage))
            {
                incompatibilities.Add(new(
                    WireIncompatibilityKind.MessageRemoved,
                    name,
                    "The message is gone, so nothing on the newer side can read what the older one sends."));
                continue;
            }

            foreach (var (number, field) in message.Fields)
            {
                CheckField(name, field, number, actualMessage, incompatibilities);
            }
        }
    }

    static void CheckField(
        string messageName,
        WireField field,
        int number,
        WireMessage actualMessage,
        List<WireIncompatibility> incompatibilities)
    {
        var path = $"{messageName}.{field.Name}";

        // Retiring a field into a reserved range is still a removal from the older side's point of view: it keeps
        // writing the number, and nothing reads it any more. Reserving protects the number from reuse; it does not
        // make the data arrive.
        if (!actualMessage.Fields.TryGetValue(number, out var actual))
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.FieldRemoved,
                path,
                $"Field number {number} is no longer declared, so what the older side writes there is dropped."));
            return;
        }

        if (!string.Equals(field.TypeName, actual.TypeName, StringComparison.Ordinal))
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.FieldTypeChanged,
                path,
                $"Field number {number} changed type from {field.TypeName} to {actual.TypeName}."));
        }

        if (field.Label != actual.Label)
        {
            // Widening a length-delimited field to repeated keeps binary decoding working - the encoding of one
            // occurrence is identical, and a singular reader takes the last one - but every generated client
            // changes the property's type, so an older client stops compiling against it. Say which it is, because
            // the two call for very different fixes.
            var widenedInPlace = field.Label == WireFieldLabel.Singular
                && actual.Label == WireFieldLabel.Repeated
                && IsLengthDelimited(field.TypeName);

            var description = widenedInPlace
                ? $"Field number {number} was widened from {Describe(field.Label)} to {Describe(actual.Label)}. Binary decoding survives it, generated code does not."
                : $"Field number {number} changed from {Describe(field.Label)} to {Describe(actual.Label)}.";

            incompatibilities.Add(new(WireIncompatibilityKind.FieldLabelChanged, path, description));
        }

        // A rename keeps binary protobuf decoding, which goes by number - but every generated client addresses the
        // field by name, and so does the JSON representation, so an older client stops seeing the value.
        if (!string.Equals(field.Name, actual.Name, StringComparison.Ordinal))
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.FieldRenamed,
                path,
                $"Field number {number} was renamed to '{actual.Name}'. Binary decoding survives it, generated code and JSON do not."));
        }

        if (!string.Equals(field.OneOf, actual.OneOf, StringComparison.Ordinal))
        {
            incompatibilities.Add(new(
                WireIncompatibilityKind.FieldOneOfChanged,
                path,
                $"Field number {number} moved from {DescribeOneOf(field.OneOf)} to {DescribeOneOf(actual.OneOf)}, changing which fields it clears."));
        }
    }

    static void CheckEnums(WireContract expected, WireContract actual, List<WireIncompatibility> incompatibilities)
    {
        foreach (var (name, @enum) in expected.Enums)
        {
            if (!actual.Enums.TryGetValue(name, out var actualEnum))
            {
                incompatibilities.Add(new(
                    WireIncompatibilityKind.EnumRemoved,
                    name,
                    "The enum is gone, so nothing on the newer side can name what the older one sends."));
                continue;
            }

            foreach (var (number, valueName) in @enum.Values)
            {
                if (!actualEnum.Values.TryGetValue(number, out var actualValueName))
                {
                    incompatibilities.Add(new(
                        WireIncompatibilityKind.EnumValueRemoved,
                        $"{name}.{valueName}",
                        $"Value {number} is no longer defined, so the older side sends a number the newer one cannot name."));
                    continue;
                }

                if (!string.Equals(valueName, actualValueName, StringComparison.Ordinal))
                {
                    incompatibilities.Add(new(
                        WireIncompatibilityKind.EnumValueRenamed,
                        $"{name}.{valueName}",
                        $"Value {number} was renamed to '{actualValueName}'."));
                }
            }
        }
    }

    static string Describe(WireMethod method) => (method.ClientStreaming, method.ServerStreaming) switch
    {
        (false, false) => "unary",
        (false, true) => "server streaming",
        (true, false) => "client streaming",
        (true, true) => "bidirectional streaming"
    };

    static string Describe(WireFieldLabel label) => label == WireFieldLabel.Repeated ? "repeated" : "singular";

    /// <summary>
    /// Determines whether a field type is length-delimited on the wire.
    /// </summary>
    /// <param name="typeName">The proto type name.</param>
    /// <returns>True when one occurrence encodes the same whether the field is singular or repeated.</returns>
    /// <remarks>
    /// Numeric scalars and enums are packed when repeated in proto3, which is a different encoding from a singular
    /// one. Length-delimited types are not packable, so one occurrence looks the same either way.
    /// </remarks>
    static bool IsLengthDelimited(string typeName) =>
        string.Equals(typeName, "string", StringComparison.Ordinal)
        || string.Equals(typeName, "bytes", StringComparison.Ordinal)
        || typeName.StartsWith('.');

    static string DescribeOneOf(string? oneOf) => oneOf is null ? "standing alone" : $"oneof '{oneOf}'";
}
