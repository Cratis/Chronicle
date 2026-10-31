// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Xml;

namespace Cratis.Chronicle.Tools.LegacyContracts;

record EvaluationRun(int ExitCode, IReadOnlyList<RowVerdict> Rows, string? Error, string Mode = "report")
{
    public static EvaluationRun Read(string manifest, string issues, string jobs, Func<IReadOnlyList<TestResult>> readTests, string? mode = null)
    {
        try
        {
            var matrix = Matrix.Parse(manifest);
            var enforcement = mode ?? matrix.Enforcement;
            Matrix.ValidateMode(enforcement);
            var rows = MatrixEvaluation.Evaluate(matrix, JsonInput.Read<OwnerIssue[]>(issues), JsonInput.Read<Dictionary<string, JobResult>>(jobs), readTests());
            return new(rows.Any(_ => _.Fails(enforcement)) ? 1 : 0, rows, null, enforcement);
        }
        catch (Exception ex) when (ex is CannotEvaluate or JsonException or XmlException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new(2, [], ex.Message);
        }
    }
}
