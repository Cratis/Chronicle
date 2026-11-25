// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const decimal = /^(-?)(\d*)(?:\.(\d*))?(?:[eE]([+-]?\d+))?$/;

const normalize = (token: string): string | undefined => {
    const match = decimal.exec(token);
    if (!match) return undefined;
    const fraction = match[3] ?? '';
    const digits = `${match[2]}${fraction}`.replace(/^0+/, '');
    if (digits === '') return '0';
    const trimmed = digits.replace(/0+$/, '');
    const exponent = Number(match[4] ?? 0) - fraction.length + (digits.length - trimmed.length);
    return `${match[1]}${trimmed}e${exponent}`;
};

/**
 * Whether a JSON number token survives a round trip through a JavaScript number. Tokens that
 * overflow, underflow or lose digits must not be compared, as they could look equal while not being so.
 * @param token The numeric token exactly as written in the JSON text.
 * @returns True when the decimal value is preserved.
 */
export const isRoundTripNumber = (token: string): boolean => {
    const value = Number(token);
    if (!Number.isFinite(value)) return false;
    const written = normalize(token);
    return written !== undefined && written === normalize(JSON.stringify(value));
};
