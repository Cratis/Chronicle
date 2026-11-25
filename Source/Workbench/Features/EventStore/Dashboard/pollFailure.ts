// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';

/**
 * Describe a failed poll without hiding a previously read value or exposing server errors.
 * @param value The retained sample, if a poll has succeeded before.
 * @param failed Whether the latest poll failed.
 * @returns A failure caption, or undefined while loading and after a successful poll.
 */
export const pollFailure = (value: unknown, failed: boolean): string | undefined => {
    if (!failed) return undefined;
    return value === undefined ? strings.eventStore.dashboard.unavailable : strings.eventStore.dashboard.stale;
};
