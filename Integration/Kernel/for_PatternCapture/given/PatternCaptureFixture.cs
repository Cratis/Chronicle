// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

namespace Cratis.Chronicle.Kernel.Integration.for_PatternCapture.given;

/// <summary>
/// A kernel with pattern capture on and its persistence interval short. Kept apart from the dormant observer fixtures
/// because the interval is a setting of the kernel, which is started once per fixture.
/// </summary>
public class PatternCaptureFixture : DormantObserverFixture;
