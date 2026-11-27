// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { type ObserverLagTarget } from './ObserverLagController';
import { type ObserverLagView } from './useObserverLag';
import { presentObserverLag } from './observerLagPresentation';
import { describeObserverLag } from './describeObserverLag';

export interface ObserverLagCellProps {
    target: ObserverLagTarget;
    view: ObserverLagView;
}

/** The lag column of an observer row. Reports itself as on screen, and shows what the view has read for the observer. */
export const ObserverLagCell = ({ target, view }: ObserverLagCellProps) => {
    const element = useRef<HTMLSpanElement>(null);
    const { trackCell } = view;
    const { eventStore, namespace, eventSequenceId, observerId } = target;

    useEffect(
        () => element.current ? trackCell(element.current, { eventStore, namespace, eventSequenceId, observerId }) : undefined,
        [trackCell, eventStore, namespace, eventSequenceId, observerId]);

    const lag = view.lagFor(target);
    const display = presentObserverLag(lag, view.now);
    const isLoaded = lag !== undefined || view.hasResult(target);

    return <span ref={element}>{describeObserverLag(display, isLoaded)}</span>;
};
