// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Configures identifier redaction for Chronicle logging.
/// </summary>
public static class IdentifierLoggingExtensions
{
    /// <summary>
    /// Register the Chronicle identifier redactor, replacing the default provider.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to configure.</param>
    /// <returns>The same <see cref="ILoggingBuilder"/> for continuation.</returns>
    public static ILoggingBuilder AddChronicleLogRedaction(this ILoggingBuilder builder)
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<IRedactorProvider, IdentifierRedactorProvider>());
        return builder;
    }
}
