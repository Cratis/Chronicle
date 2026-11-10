// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Parses the driver command line without starting infrastructure.
/// </summary>
internal static class RunOptionsParser
{
    static readonly string[] _valueOptions = ["--seed", "--duration", "--settle", "--faults", "--output"];

    /// <summary>
    /// Parses and validates options.
    /// </summary>
    /// <param name="args">Driver options.</param>
    /// <returns>Validated options.</returns>
    public static RunOptions Parse(string[] args)
    {
        var values = Values(args);
        var seed = values.TryGetValue("--seed", out var supplied) ? int.Parse(supplied, CultureInfo.InvariantCulture) : RandomNumberGenerator.GetInt32(int.MaxValue);
        var faults = ParseFaults(values.GetValueOrDefault("--faults", "all"));
        var output = values.GetValueOrDefault("--output", Path.Combine("Chaos", "runs", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{seed}"));
        return new(seed, Duration(values.GetValueOrDefault("--duration", "2m")), Duration(values.GetValueOrDefault("--settle", "60s")), faults, output, values.ContainsKey("--keep-containers"), values.ContainsKey("--self-test-violation"));
    }

    static Dictionary<string, string> Values(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            var flag = args[index];
            if (flag == "--keep-containers" || flag == "--self-test-violation")
            {
                values.Add(flag, "true");
                continue;
            }
            if (!_valueOptions.Contains(flag))
            {
                throw new ArgumentException($"Unknown option: {flag}");
            }
            if (++index == args.Length)
            {
                throw new ArgumentException($"Missing value for {flag}");
            }
            values.Add(flag, args[index]);
        }
        return values;
    }

    static TimeSpan Duration(string text)
    {
        if (text.Length < 2) throw new ArgumentException($"Invalid duration: {text}");
        var amount = PositiveAmount(text[..^1]);
        return text[^1] switch
        {
            's' => TimeSpan.FromSeconds(amount),
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            _ => throw new ArgumentException($"Invalid duration unit: {text}")
        };
    }

    static double PositiveAmount(string text)
    {
        if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)) throw new ArgumentException($"Invalid duration amount: {text}");
        if (!double.IsFinite(amount)) throw new ArgumentException($"Duration must be finite: {text}");
        if (amount <= 0) throw new ArgumentException($"Duration must be positive: {text}");
        return amount;
    }

    static FaultKind[] ParseFaults(string text)
    {
        if (text == "all") return Enum.GetValues<FaultKind>();
        if (text == "none") return [];
        return text.Split(',').Select(name => Enum.TryParse<FaultKind>(name, true, out var kind) && Enum.IsDefined(kind)
            ? kind : throw new ArgumentException($"Unknown fault: {name}")).Distinct().ToArray();
    }
}
