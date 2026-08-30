// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface Rgb {
    red: number;
    green: number;
    blue: number;
}

/**
 * Stops along the viridis color map.
 *
 * Confidence is sequential - ordered, one-dimensional, with no meaningful midpoint - so it wants a sequential
 * scale rather than a rainbow. Rainbow ramps are not monotonic in lightness, which invents boundaries where the
 * data has none and collapses under the common forms of color blindness. Viridis is monotonic in lightness across
 * its whole range, so the ordering survives both a grayscale print and a color-blind reader, and it spans enough
 * of the color space to keep neighboring values apart.
 *
 * Sampled evenly from the reference map; interpolating between them is close enough to the real thing at the
 * handful of steps a grid this size can show.
 */
const viridis: Rgb[] = [
    { red: 68, green: 1, blue: 84 },
    { red: 72, green: 40, blue: 120 },
    { red: 62, green: 74, blue: 137 },
    { red: 49, green: 104, blue: 142 },
    { red: 38, green: 130, blue: 142 },
    { red: 31, green: 158, blue: 137 },
    { red: 53, green: 183, blue: 121 },
    { red: 109, green: 205, blue: 89 },
    { red: 180, green: 222, blue: 44 },
    { red: 253, green: 231, blue: 37 }
];

const clamp = (value: number) => Math.min(Math.max(value, 0), 1);

const mix = (from: number, to: number, amount: number) => Math.round(from + ((to - from) * amount));

/**
 * The color for a value between 0 and 1.
 *
 * @param value The value to get the color for.
 * @returns The interpolated {@link Rgb}.
 */
export const colorFor = (value: number): Rgb => {
    const position = clamp(value) * (viridis.length - 1);
    const lower = Math.floor(position);
    const upper = Math.min(lower + 1, viridis.length - 1);
    const amount = position - lower;

    return {
        red: mix(viridis[lower].red, viridis[upper].red, amount),
        green: mix(viridis[lower].green, viridis[upper].green, amount),
        blue: mix(viridis[lower].blue, viridis[upper].blue, amount)
    };
};

export const toCss = ({ red, green, blue }: Rgb) => `rgb(${red}, ${green}, ${blue})`;

/**
 * The relative luminance of a color, per the WCAG definition.
 *
 * @param color The {@link Rgb} to measure.
 * @returns The luminance, 0 for black and 1 for white.
 */
export const luminanceOf = ({ red, green, blue }: Rgb) => {
    const channel = (value: number) => {
        const normalized = value / 255;
        return normalized <= 0.03928 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
    };

    return (0.2126 * channel(red)) + (0.7152 * channel(green)) + (0.0722 * channel(blue));
};

/**
 * The label color to put on top of a cell.
 *
 * Viridis runs from near-black to bright yellow, so a single fixed label color is unreadable at one end of the
 * scale whichever end you pick. Choosing per cell from its own luminance keeps every label legible.
 *
 * @param background The cell's {@link Rgb}.
 * @returns The color to draw the label in.
 */
export const labelColorFor = (background: Rgb) => (luminanceOf(background) > 0.45 ? '#101010' : '#ffffff');

/**
 * The evenly spaced stops to draw a legend from.
 *
 * @param steps How many stops to produce.
 * @returns The stops, from the bottom of the scale to the top.
 */
export const legendStops = (steps: number): Rgb[] =>
    steps <= 1
        ? [colorFor(0)]
        : Array.from({ length: steps }, (_, index) => colorFor(index / (steps - 1)));
