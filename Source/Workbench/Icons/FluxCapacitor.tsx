// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The Time Machine icon.
 *
 * Sourced from the Noun Project — "Created by Fajriah Robiatul Adawiah". The download
 * carried that credit as two `<text>` nodes below the artwork, outside the `0 0 32 32`
 * viewBox: they never rendered, but they were read out as the accessible name of every
 * button using the icon and forced the box a quarter taller than the glyph to make room
 * for them. The credit lives here and in CREDITS.md instead.
 *
 * Decorative — the control that renders it supplies the accessible name.
 */
export const FluxCapacitor = ({ size = 20 }: { size?: number }) => (
    <svg xmlns="http://www.w3.org/2000/svg"
        width={size}
        height={size}
        fill="currentColor"
        viewBox="0 0 32 32"
        aria-hidden="true"
        focusable="false">
        <path d="M27,2H5c-1.65,0-3,1.35-3,3V27c0,1.65,1.35,3,3,3H27c1.65,0,3-1.35,3-3V5c0-1.65-1.35-3-3-3Zm1,25c0,.55-.45,1-1,1H5c-.55,0-1-.45-1-1V5c0-.55,.45-1,1-1H27c.55,0,1,.45,1,1V27ZM22,6.5c-1.93,0-3.5,1.57-3.5,3.5,0,.57,.15,1.1,.39,1.58l-2.89,2.17-2.89-2.17c.24-.48,.39-1.01,.39-1.58,0-1.93-1.57-3.5-3.5-3.5s-3.5,1.57-3.5,3.5,1.57,3.5,3.5,3.5c.62,0,1.2-.18,1.71-.47l3.29,2.47v3.16c-1.44,.43-2.5,1.76-2.5,3.34,0,1.93,1.57,3.5,3.5,3.5s3.5-1.57,3.5-3.5c0-1.58-1.06-2.9-2.5-3.34v-3.16l3.29-2.47c.51,.29,1.09,.47,1.71,.47,1.93,0,3.5-1.57,3.5-3.5s-1.57-3.5-3.5-3.5Zm-12,5c-.83,0-1.5-.67-1.5-1.5s.67-1.5,1.5-1.5,1.5,.67,1.5,1.5-.67,1.5-1.5,1.5Zm7.5,10.5c0,.83-.67,1.5-1.5,1.5s-1.5-.67-1.5-1.5,.67-1.5,1.5-1.5,1.5,.67,1.5,1.5Zm4.5-10.5c-.83,0-1.5-.67-1.5-1.5s.67-1.5,1.5-1.5,1.5,.67,1.5,1.5-.67,1.5-1.5,1.5Z" />
    </svg>
);
