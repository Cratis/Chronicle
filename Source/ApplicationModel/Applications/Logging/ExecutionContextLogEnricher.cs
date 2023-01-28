// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Serilog.Core;
using Serilog.Events;

namespace Aksio.Cratis.Applications.Logging;

/// <summary>
/// Represents an implementation of <see cref="ILogEventEnricher"/> for enriching log events with values from the execution context.
/// </summary>
public class ExecutionContextLogEnricher : ILogEventEnricher
{
    /// <summary>
    /// The name of the property for the microservice identifier.
    /// </summary>
    public const string MicroserviceIdProperty = "MicroserviceId";

    /// <summary>
    /// The name of the property for the tenant identifier.
    /// </summary>
    public const string TenantIdProperty = "TenantId";

    /// <summary>
    /// The name of the property for the correlation identifier.
    /// </summary>
    public const string CorrelationIdProperty = "CorrelationId";

    /// <summary>
    /// The name of the property for the causation identifier.
    /// </summary>
    public const string CausationIdProperty = "CausationId";

    /// <summary>
    /// The name of the property for the caused by identifier.
    /// </summary>
    public const string CausedByIdProperty = "CausedById";

    /// <inheritdoc/>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (ExecutionContextManager.HasCurrent)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(MicroserviceIdProperty, ExecutionContextManager.GetCurrent().MicroserviceId.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(TenantIdProperty, ExecutionContextManager.GetCurrent().TenantId.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CorrelationIdProperty, ExecutionContextManager.GetCurrent().CorrelationId.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CausationIdProperty, ExecutionContextManager.GetCurrent().CausationId.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CausedByIdProperty, ExecutionContextManager.GetCurrent().CausedBy.Value));
        }
    }
}
