// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

/// <summary>
/// Switches the application-side extension points in this folder on for the current asynchronous flow only. They are
/// discovered by every command pipeline built in this assembly, so they stay inert everywhere else.
/// </summary>
public static class ApplicationExtensionPoints
{
    static readonly AsyncLocal<ApplicationExtensionPointsRecorder?> _recorder = new();

    public static ApplicationExtensionPointsRecorder? Recorder => _recorder.Value;

    public static void Activate(ApplicationExtensionPointsRecorder recorder) => _recorder.Value = recorder;
}
