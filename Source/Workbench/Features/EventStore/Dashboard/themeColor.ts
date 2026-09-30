// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Resolve a design token to the color it holds right now.
 * @param token The name of the custom property, including the leading dashes.
 * @param fallback The color to use when the token is not defined.
 * @returns The color.
 * @remarks Canvas drawing takes colors, not custom properties, so a chart has to resolve the token itself to follow
 * the theme rather than hard-coding a palette that fights it.
 */
export const themeColor = (token: string, fallback: string): string => {
    if (typeof document === 'undefined') return fallback;
    const value = getComputedStyle(document.documentElement).getPropertyValue(token).trim();
    return value.length > 0 ? value : fallback;
};

/**
 * Make a color translucent.
 * @param color The color, in any CSS syntax.
 * @param alpha The opacity, between 0 and 1.
 * @returns The translucent color as `rgba()`.
 * @remarks Tokens can hold any CSS color syntax, while chart.js only understands a few of them - so the color is
 * painted onto a single pixel and read back, which leaves the browser to do the parsing.
 */
export const withAlpha = (color: string, alpha: number): string => {
    const context = typeof document === 'undefined' ? null : document.createElement('canvas').getContext('2d', { willReadFrequently: true });
    if (!context) return color;

    context.fillStyle = color;
    context.fillRect(0, 0, 1, 1);
    const [red, green, blue] = context.getImageData(0, 0, 1, 1).data;
    return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
};
