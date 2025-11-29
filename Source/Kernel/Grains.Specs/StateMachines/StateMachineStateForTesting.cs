// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.StateMachines;

public class StateMachineStateForTesting : StateMachineState
{
    public string Something { get; set; } = string.Empty;

    public override string ToString() => Something;

    public override bool Equals(object obj) => Something.Equals(((StateMachineStateForTesting)obj).Something);

    public override int GetHashCode() => Something.GetHashCode();
}
