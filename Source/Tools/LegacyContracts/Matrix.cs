// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

record Matrix(string Enforcement, MatrixRow[] Rows)
{
    public static Matrix Parse(string json)
    {
        var matrix = JsonInput.Read<Matrix>(json);
        ValidateMode(matrix.Enforcement);
        if (matrix.Rows is null || matrix.Rows.Length == 0 || matrix.Rows.Any(_ => _ is null)
            || matrix.Rows.Select(_ => _.Id).Distinct(StringComparer.Ordinal).Count() != matrix.Rows.Length)
        {
            throw new CannotEvaluate("The manifest must declare nonempty, uniquely named rows.");
        }

        foreach (var row in matrix.Rows)
        {
            Validate(row);
        }

        return matrix;
    }

    public static void ValidateMode(string mode)
    {
        if (!string.Equals(mode, "report", StringComparison.Ordinal) && !string.Equals(mode, "release", StringComparison.Ordinal))
        {
            throw new CannotEvaluate("Enforcement must be 'report' or 'release'.");
        }
    }

    static void Validate(MatrixRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Id) || string.IsNullOrWhiteSpace(row.Proves)
            || row.Owners?.Any(_ => _ <= 0) != false
            || row.Jobs?.Any(string.IsNullOrWhiteSpace) != false
            || row.Specs?.Any(_ => _ is null || string.IsNullOrWhiteSpace(_.Selector) || _.Minimum < 1) != false
            || row.PlantedDefects?.Any(_ => _ is null || string.IsNullOrWhiteSpace(_.Defect) || string.IsNullOrWhiteSpace(_.Spec)) != false
            || row.RequiredProofs?.Any(string.IsNullOrWhiteSpace) != false)
        {
            throw new CannotEvaluate($"Invalid manifest row '{row.Id}'.");
        }

        if ((!string.Equals(row.Status, "runnable", StringComparison.Ordinal) && !string.Equals(row.Status, "pending-owner", StringComparison.Ordinal))
            || (string.Equals(row.Status, "runnable", StringComparison.Ordinal) && (row.Jobs.Length == 0 || row.Specs.Length == 0 || row.PlantedDefects.Length == 0))
            || (string.Equals(row.Status, "pending-owner", StringComparison.Ordinal) && row.RequiredProofs.Length == 0))
        {
            throw new CannotEvaluate($"Row '{row.Id}' needs a valid status and its required proof declarations.");
        }
    }
}
