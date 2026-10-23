// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

record Options(string? Manifest, string? Issues, string? Jobs, string? Trx, string? Mode, bool GitHub, bool SelfTest)
{
    public const string Usage = "LegacyContracts --manifest <json> --issues <json> --jobs <json> --trx <directory> [--mode report|release] [--github] | --self-test";

    static readonly string[] _requiredOptions = ["--manifest", "--issues", "--jobs", "--trx"];

    public static Options Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var github = false;
        var selfTest = false;
        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (string.Equals(option, "--github", StringComparison.Ordinal) || string.Equals(option, "--self-test", StringComparison.Ordinal))
            {
                github |= string.Equals(option, "--github", StringComparison.Ordinal);
                selfTest |= string.Equals(option, "--self-test", StringComparison.Ordinal);
            }
            else if (_requiredOptions.Contains(option, StringComparer.Ordinal) || string.Equals(option, "--mode", StringComparison.Ordinal))
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal) || !values.TryAdd(option, args[index]))
                {
                    throw new CannotEvaluate($"'{option}' needs one value.");
                }
            }
            else
            {
                throw new CannotEvaluate($"Unknown argument '{option}'.");
            }
        }

        if (!selfTest && _requiredOptions.Any(_ => !values.ContainsKey(_)))
        {
            throw new CannotEvaluate(Usage);
        }

        var mode = values.GetValueOrDefault("--mode");
        if (mode is not null)
        {
            Matrix.ValidateMode(mode);
        }

        return new(values.GetValueOrDefault("--manifest"), values.GetValueOrDefault("--issues"), values.GetValueOrDefault("--jobs"), values.GetValueOrDefault("--trx"), mode, github, selfTest);
    }
}
