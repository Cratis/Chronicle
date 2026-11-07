// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintStoreKey;

public class when_serializing_as_json : given.secret_material
{
    Exception? _directError;
    Exception? _recordError;
    string? _directOutput;
    string? _recordOutput;
    (Exception? Error, string Output) _directWriter;
    (Exception? Error, string Output) _recordWriter;

    void Because()
    {
        _directError = Catch.Exception(() => _directOutput = JsonSerializer.Serialize(_material));
        _recordError = Catch.Exception(() => _recordOutput = JsonSerializer.Serialize(_containing));
        _directWriter = SerializeToWriter(_material);
        _recordWriter = SerializeToWriter(_containing);
    }

    [Fact] void should_explicitly_reject_direct_serialization() => AssertPolicyRejection(_directError);
    [Fact] void should_explicitly_reject_containing_record_serialization() => AssertPolicyRejection(_recordError);
    [Fact] void should_return_no_direct_json_output() => _directOutput.ShouldBeNull();
    [Fact] void should_return_no_containing_record_json_output() => _recordOutput.ShouldBeNull();
    [Fact] void should_not_disclose_material_in_direct_diagnostics() => AssertNoMaterial(_directError);
    [Fact] void should_not_disclose_material_in_containing_record_diagnostics() => AssertNoMaterial(_recordError);
    [Fact] void should_explicitly_reject_direct_writer_serialization() => AssertPolicyRejection(_directWriter.Error);
    [Fact] void should_explicitly_reject_containing_record_writer_serialization() => AssertPolicyRejection(_recordWriter.Error);
    [Fact] void should_write_no_bytes_for_material() => _directWriter.Output.ShouldEqual(string.Empty);
    [Fact] void should_stop_before_writing_a_containing_records_material() => _recordWriter.Output.ShouldEqual($"{{\"KeyId\":{JsonSerializer.Serialize(_containing.KeyId)},\"Material\":");

    static (Exception? Error, string Output) SerializeToWriter<T>(T value)
    {
        using var output = new MemoryStream();
        using var writer = new Utf8JsonWriter(output);
        var error = Catch.Exception(() => JsonSerializer.Serialize(writer, value));
        writer.Flush();
        return (error, Encoding.UTF8.GetString(output.ToArray()));
    }

    static void AssertPolicyRejection(Exception? error)
    {
        error.ShouldBeOfExactType<NotSupportedException>();
        error!.Message.StartsWith("Unique-constraint store key material cannot be serialized or deserialized as JSON.", StringComparison.Ordinal).ShouldBeTrue();
    }

    void AssertNoMaterial(Exception? error)
    {
        var diagnostic = error!.ToString();
        var bytes = _material.CopyBytes();
        diagnostic.Contains(Convert.ToBase64String(bytes), StringComparison.Ordinal).ShouldBeFalse();
        diagnostic.Contains(Convert.ToHexString(bytes), StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        diagnostic.Contains(JsonSerializer.Serialize(bytes.Select(value => (int)value)), StringComparison.Ordinal).ShouldBeFalse();
    }
}
