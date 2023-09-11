// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class StateMachineState
{
    public string Something { get; set; } = string.Empty;

    public override string ToString() => Something;

    public override bool Equals(object obj) => Something.Equals(((StateMachineState)obj).Something);

    public override int GetHashCode() => Something.GetHashCode();
}
