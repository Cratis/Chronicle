// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Records through a value-type constrained call without boxing, delegates or copying the tag list.
/// </summary>
/// <typeparam name="T">The measurement type.</typeparam>
internal interface IMetricRecorder<T>
    where T : struct
{
    /// <summary>Records a measurement with the supplied tags.</summary>
    /// <param name="value">The measurement.</param>
    /// <param name="tags">The measurement tags, passed by readonly reference.</param>
    void Record(T value, in TagList tags);
}
