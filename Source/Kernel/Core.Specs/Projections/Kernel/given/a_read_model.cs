// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Kernel.given;

/// <summary>
/// A read model shape for the kernel projection builder specifications to declare against.
/// </summary>
public class a_read_model
{
    public string EventType { get; set; } = string.Empty;

    public string Namespace { get; set; } = string.Empty;

    public int Count { get; set; }

    public int Failed { get; set; }
}
