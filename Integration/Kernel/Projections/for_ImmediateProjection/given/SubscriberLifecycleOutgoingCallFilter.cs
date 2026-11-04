// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Orders the subscriber's unsubscribe, the definition notification and the resubscribe around the projection grain.
/// </summary>
/// <param name="gate">The test's lifecycle gate.</param>
public class SubscriberLifecycleOutgoingCallFilter(SubscriberLifecycleGate gate) : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (IsCall(context, "IProjection", "UnsubscribeDefinitionsChanged") && gate.TryEnterUnsubscribe())
        {
            if (gate.DropUnsubscribe)
            {
                // The unsubscribe is lost: the failing OnDeactivateAsync is logged by Orleans and the grain still deactivates.
                throw new InvalidOperationException("The test dropped the definition unsubscribe.");
            }

            await gate.WaitForUnsubscribeRelease();
            await context.Invoke();
            gate.UnsubscribeCompleted();
            return;
        }

        if (IsCall(context, "INotifyProjectionDefinitionsChanged", "OnProjectionDefinitionsChanged"))
        {
            gate.NotificationWasSent();
        }
        else if (IsCall(context, "IProjection", "SubscribeDefinitionsChanged") && gate.TryEnterResubscribe())
        {
            await context.Invoke();
            gate.ResubscribeCompleted();
            return;
        }

        await context.Invoke();
    }

    static bool IsCall(IOutgoingGrainCallContext context, string interfaceName, string methodName) =>
        context.Request.GetInterfaceType().FullName == $"Cratis.Chronicle.Projections.{interfaceName}" && context.MethodName == methodName;
}
