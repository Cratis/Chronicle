// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { formatAge, type ObserverLagDisplay } from './observerLagPresentation';

const lagStrings = strings.eventStore.namespaces.observers.lag;

/** The words for a measurement's state: a status when it measured the observer, a reason when it could not. */
export const describeObserverLagState = (state: ObserverLagDisplay['state']): string => lagStrings.states[state];

/** The sampled lag of one observer as a short line: its state, the distance when measured, and how old the sample is. */
export const describeObserverLag = (display: ObserverLagDisplay | undefined, isLoaded: boolean): string => {
    if (!display) return isLoaded ? lagStrings.unavailable : lagStrings.loading;

    const parts = [describeObserverLagState(display.state)];
    if (display.distance !== undefined) parts.push(lagStrings.distance.replace('{distance}', display.distance));
    if (display.ageSeconds !== undefined) parts.push(lagStrings.age.replace('{age}', formatAge(display.ageSeconds)));
    return parts.join(' · ');
};
