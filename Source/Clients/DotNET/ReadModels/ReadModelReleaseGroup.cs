// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The read model properties that release under one declared subject.
/// </summary>
/// <param name="SubjectProperty">The property holding the subject the group releases under.</param>
/// <param name="Properties">The properties declared to release under it.</param>
internal sealed record ReadModelReleaseGroup(PropertyInfo SubjectProperty, IReadOnlyList<PropertyInfo> Properties);
