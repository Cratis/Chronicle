// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Aksio.Cratis.Applications.ProxyGenerator;

/// <summary>
/// Holds <see cref="Diagnostic"/> outputs to use.
/// </summary>
public static class Diagnostics
{
    /// <summary>
    /// The <see cref="Diagnostic"/> to report when the output path is missing.
    /// </summary>
    public static readonly Diagnostic MissingOutputPath = Diagnostic.Create(
        new DiagnosticDescriptor(
            "AKSIO0001",
            "Missing output path",
            "Missing output path for generating proxies to. Add <AksioProxyOutput/> to your .csproj file. Will not output proxies.",
            "Generation",
            DiagnosticSeverity.Warning,
            true),
        default);

    /// <summary>
    /// The <see cref="Diagnostic"/> to report when the output path is missing.
    /// </summary>
    public static readonly Func<string, Diagnostic> UnableToResolveModelType = (string queryName) => Diagnostic.Create(
        new DiagnosticDescriptor(
            "AKSIO0003",
            "Unable to resolve model type",
            $"Unable to resolve model type from '{queryName}'.",
            "Generation",
            DiagnosticSeverity.Error,
            true),
        default);

    /// <summary>
    /// The <see cref="Diagnostic"/> to report when the output path is missing.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns><see cref="Diagnostics"/> for reporting.</returns>
    public static Diagnostic UnknownError(Exception exception) => Diagnostic.Create(
        new DiagnosticDescriptor(
            "AKSIO0000",
            "Error during proxy generation.",
            "Error '{0}' happened during proxy generation.",
            "Generation",
            DiagnosticSeverity.Warning,
            true,
            "Stack:\n{1}"),
        default,
        messageArgs: new object[] { exception.Message, exception.StackTrace });
}
