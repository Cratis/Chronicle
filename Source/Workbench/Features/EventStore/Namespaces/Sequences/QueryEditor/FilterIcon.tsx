// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The funnel the pivot viewer marks its filter button with.
 *
 * Drawn here rather than taken from an icon set so that the two ways of filtering a sequence - the
 * pivot viewer and the query editor - carry the exact same mark.
 * @returns The rendered icon.
 */
export const FilterIcon = () => (
    <svg
        width='18'
        height='18'
        viewBox='0 0 24 24'
        fill='none'
        stroke='currentColor'
        strokeWidth='2'
        strokeLinecap='round'
        strokeLinejoin='round'
        aria-hidden='true'>
        <polygon points='22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3' />
    </svg>
);
