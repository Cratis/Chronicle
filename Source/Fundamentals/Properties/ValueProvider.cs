// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Properties;

/// <summary>
/// Represents the delegate for providing a value from an object.
/// </summary>
/// <typeparam name="T">Type of the source the value provider is for.</typeparam>
/// <param name="source">Source to get from.</param>
/// <returns>Resolved value.</returns>
public delegate object ValueProvider<T>(T source);
