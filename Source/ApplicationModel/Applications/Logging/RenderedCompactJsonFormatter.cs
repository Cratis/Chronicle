// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Aksio.Cratis.Applications.Logging;

/// <summary>
/// An <see cref="ITextFormatter"/> that writes events in a compact JSON format, for consumption in environments
/// without message template support. Message templates are rendered into text and a hashed event id is included.
/// </summary>
/// <remarks>
/// Based on and adapted from https://github.com/serilog/serilog-formatting-compact.
/// </remarks>
public class RenderedCompactJsonFormatter : ITextFormatter
{
    readonly JsonValueFormatter _valueFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderedCompactJsonFormatter"/> class.
    /// <see cref="LogEventPropertyValue"/>s on the event.
    /// </summary>
    /// <param name="valueFormatter">A value formatter, or null.</param>
    public RenderedCompactJsonFormatter(JsonValueFormatter? valueFormatter = null)
    {
        _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
    }

    /// <summary>
    /// Format the log event into the output.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
    /// <exception cref="ArgumentNullException">If any out the parameters are null.</exception>
    public static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        ArgumentNullException.ThrowIfNull(logEvent, nameof(logEvent));
        ArgumentNullException.ThrowIfNull(output, nameof(output));
        ArgumentNullException.ThrowIfNull(valueFormatter, nameof(valueFormatter));

        output.Write("{\"Time\":\"");
        output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
        output.Write("\",\"Message\":");
        var message = logEvent.MessageTemplate.Render(logEvent.Properties);
        JsonValueFormatter.WriteQuotedJsonString(message, output);
        output.Write(",\"MessageHash\":\"");
        var id = EventIdHash.Compute(logEvent.MessageTemplate.Text);
        output.Write(id.ToString("x8"));
        output.Write('"');

        if (logEvent.Level != LogEventLevel.Information)
        {
            output.Write(",\"Level\":\"");
            output.Write(Enum.GetName(typeof(LogEventLevel), logEvent.Level));
            output.Write('\"');
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"Exception\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        foreach (var property in logEvent.Properties)
        {
            var name = property.Key;
            if (name.Length > 0 && name[0] == '@')
            {
                // Escape first '@' by doubling
                name = '@' + name;
            }

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }

    /// <summary>
    /// Format the log event into the output. Subsequent events will be newline-delimited.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }
}
