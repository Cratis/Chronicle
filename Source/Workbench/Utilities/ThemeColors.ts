// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface ThemeColors {
    primaryColor: string;
    primaryColorText: string;
    primary500: string;
    surfaceGround: string;
    surfaceCard: string;
    surfaceSection: string;
    surfaceOverlay: string;
    surfaceBorder: string;
    textColor: string;
    textColorSecondary: string;
    highlightBg: string;
    maskbg: string;
    focusRing: string;
}

const cssVariableMap: Record<keyof ThemeColors, string> = {
    primaryColor: '--primary-color',
    primaryColorText: '--primary-color-text',
    primary500: '--primary-500',
    surfaceGround: '--surface-ground',
    surfaceCard: '--surface-card',
    surfaceSection: '--surface-section',
    surfaceOverlay: '--surface-overlay',
    surfaceBorder: '--surface-border',
    textColor: '--text-color',
    textColorSecondary: '--text-color-secondary',
    highlightBg: '--highlight-bg',
    maskbg: '--mask-bg',
    focusRing: '--focus-ring',
};

export const getThemeColors = (): Partial<ThemeColors> => {
    const root = document.documentElement;
    const computedStyle = getComputedStyle(root);

    const colors: Partial<ThemeColors> = {};

    for (const [key, cssVariable] of Object.entries(cssVariableMap)) {
        const value = computedStyle.getPropertyValue(cssVariable).trim();
        if (value) {
            colors[key as keyof ThemeColors] = value;
        }
    }

    return colors as ThemeColors;
};
