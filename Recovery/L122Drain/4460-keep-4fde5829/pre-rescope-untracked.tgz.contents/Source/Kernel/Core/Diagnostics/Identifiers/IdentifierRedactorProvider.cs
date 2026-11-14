// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Supplies the configured redactor for Chronicle's identifier classification.
/// </summary>
/// <param name="options">The Chronicle configuration.</param>
public sealed class IdentifierRedactorProvider(IOptions<ChronicleOptions> options) : IRedactorProvider
{
    readonly Lazy<IdentifierRedactor> _redactor = new(() => new IdentifierRedactor(new IdentifierTelemetry(options.Value.Telemetry)));

    /// <inheritdoc/>
    public Redactor GetRedactor(DataClassificationSet classifications) =>
        classifications.Equals(new DataClassificationSet(ChronicleDataClassifications.EventSourceIdentifier))
            ? _redactor.Value
            : NullRedactor.Instance;
}
