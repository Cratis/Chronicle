// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines the grain that subscribes pattern capture in newly added namespaces of an event store.
/// </summary>
public interface IPatternCaptureSubscriptions : IGrainWithStringKey;
