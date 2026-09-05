// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Cuts;

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// Computes the deterministic <see cref="ReadModelCutId"/> for a <see cref="ReadModelCutRequest"/>.
/// </summary>
public static class ReadModelCutIdCalculator
{
    static readonly byte[] _domain = Encoding.ASCII.GetBytes("cratis.chronicle/read-model-cut-id");

    /// <summary>
    /// Calculates the deterministic id for a request - the same request, field for field, always produces the
    /// same id, so a repeated request resolves to the same manifest instead of capturing the same content twice.
    /// </summary>
    /// <param name="request">The <see cref="ReadModelCutRequest"/> to calculate the id for.</param>
    /// <returns>The deterministic <see cref="ReadModelCutId"/>.</returns>
    public static ReadModelCutId Calculate(ReadModelCutRequest request)
    {
        using var stream = new MemoryStream();
        stream.Write(_domain);

        WriteText(stream, request.EventStore.Value);
        WriteText(stream, request.Namespace.Value);

        foreach (var cut in request.Cuts.OrderBy(_ => _.EventSequenceId.Value, StringComparer.Ordinal))
        {
            WriteText(stream, cut.EventSequenceId.Value);
            WriteUInt64(stream, cut.Position.Value);
        }

        foreach (var readModel in request.Selection.OrderBy(_ => _.Value, StringComparer.Ordinal))
        {
            WriteText(stream, readModel.Value);
        }

        var hash = SHA256.HashData(stream.ToArray());
        return new ReadModelCutId(new Guid(hash.AsSpan(0, 16)));
    }

    static void WriteText(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt64(stream, (ulong)bytes.Length);
        stream.Write(bytes);
    }

    static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }
}
