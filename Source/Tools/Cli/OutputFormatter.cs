// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Cli.Json;
using Cratis.Json;
using Spectre.Console;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Provides output formatting for CLI commands in json, text (Spectre table), or plain formats.
/// </summary>
public static class OutputFormatter
{
    static JsonSerializerOptions _jsonOptions = CreateDefaultOptions();

    /// <summary>
    /// Configures the JSON serializer options by building on top of the provided base options
    /// from <see cref="ChronicleOptions.JsonSerializerOptions"/>, adding CLI-specific converters.
    /// </summary>
    /// <param name="baseOptions">The base <see cref="JsonSerializerOptions"/> from the Chronicle client.</param>
    public static void Configure(JsonSerializerOptions baseOptions)
    {
        _jsonOptions = new JsonSerializerOptions(baseOptions)
        {
            WriteIndented = true
        };
        AddCliConverters(_jsonOptions);
    }

    /// <summary>
    /// Writes data to the console in the specified format.
    /// </summary>
    /// <typeparam name="T">The type of data items.</typeparam>
    /// <param name="format">The output format (json, text, or plain).</param>
    /// <param name="data">The data to write.</param>
    /// <param name="columns">Column definitions for tabular output.</param>
    /// <param name="getRow">Function to extract row values from each data item.</param>
    public static void Write<T>(string format, IEnumerable<T> data, string[] columns, Func<T, string[]> getRow)
    {
        switch (format)
        {
            case "json":
                WriteJson(data);
                break;
            case "plain":
                WritePlain(data, columns, getRow);
                break;
            default:
                WriteTable(data, columns, getRow);
                break;
        }
    }

    /// <summary>
    /// Writes a single object to the console in the specified format.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="format">The output format.</param>
    /// <param name="data">The object to write.</param>
    /// <param name="render">Function to render the object as text for non-json formats.</param>
    public static void WriteObject<T>(string format, T data, Action<T>? render = null)
    {
        if (format == "json")
        {
            WriteJsonSafe(data);
            return;
        }

        if (render is not null)
        {
            render(data);
            return;
        }

        WriteJsonSafe(data);
    }

    /// <summary>
    /// Writes a simple message to the console.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="message">The message text.</param>
    public static void WriteMessage(string format, string message)
    {
        if (format == "json")
        {
            var json = JsonSerializer.Serialize(new { message }, _jsonOptions);
            Console.WriteLine(json);
            return;
        }

        AnsiConsole.MarkupLine($"[green]{message.EscapeMarkup()}[/]");
    }

    /// <summary>
    /// Writes an error message and exits with the given code.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="error">The error message.</param>
    /// <param name="suggestion">An optional suggestion for resolution.</param>
    public static void WriteError(string format, string error, string? suggestion = null)
    {
        if (format == "json")
        {
            var errorObj = new Dictionary<string, string> { ["error"] = error };
            if (suggestion is not null)
            {
                errorObj["suggestion"] = suggestion;
            }

            var json = JsonSerializer.Serialize(errorObj, _jsonOptions);
            Console.Error.WriteLine(json);
            return;
        }

        AnsiConsole.MarkupLine($"[red]Error:[/] {error.EscapeMarkup()}");
        if (suggestion is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]Suggestion:[/] {suggestion.EscapeMarkup()}");
        }
    }

    static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new EnumConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        AddCliConverters(options);

        return options;
    }

    static void AddCliConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new ContractsEventTypeFromDefinitionsDictionaryConverter());
        options.Converters.Add(new ContractsEventTypeJoinDefinitionsDictionaryConverter());
        options.Converters.Add(new ContractsEventTypeRemovedWithDefinitionsDictionaryConverter());
        options.Converters.Add(new ContractsEventTypeRemovedWithJoinDefinitionsDictionaryConverter());
    }

    static void WriteJson<T>(IEnumerable<T> data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(json);
        }
        catch (NotSupportedException ex)
        {
            WriteError("json", $"Failed to serialize output as JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Safely serializes a single object to JSON, catching serialization errors.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="data">The object to serialize.</param>
    static void WriteJsonSafe<T>(T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            Console.WriteLine(json);
        }
        catch (NotSupportedException ex)
        {
            WriteError("json", $"Failed to serialize output as JSON: {ex.Message}");
        }
    }

    static void WriteTable<T>(IEnumerable<T> data, string[] columns, Func<T, string[]> getRow)
    {
        var table = new Table();
        foreach (var column in columns)
        {
            table.AddColumn(column);
        }

        foreach (var item in data)
        {
            table.AddRow(getRow(item));
        }

        AnsiConsole.Write(table);
    }

    static void WritePlain<T>(IEnumerable<T> data, string[] columns, Func<T, string[]> getRow)
    {
        Console.WriteLine(string.Join('\t', columns));
        foreach (var item in data)
        {
            Console.WriteLine(string.Join('\t', getRow(item)));
        }
    }
}
