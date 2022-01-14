// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createTheme, loadTheme } from '@fluentui/react/lib/Styling';
import { initializeIcons } from '@fluentui/font-icons-mdl2';

import './theme.scss';

initializeIcons();


const myTheme = createTheme({
    palette: {
        themePrimary: '#007bff',
        themeLighterAlt: '#00050a',
        themeLighter: '#001429',
        themeLight: '#00254d',
        themeTertiary: '#004a99',
        themeSecondary: '#006ce0',
        themeDarkAlt: '#1988ff',
        themeDark: '#3d9bff',
        themeDarker: '#70b5ff',
        neutralLighterAlt: '#1f2b32',
        neutralLighter: '#1f2a31',
        neutralLight: '#1d282f',
        neutralQuaternaryAlt: '#1b262c',
        neutralQuaternary: '#1a242a',
        neutralTertiaryAlt: '#192328',
        neutralTertiary: '#e1e3e4',
        neutralSecondary: '#e6e8e8',
        neutralPrimaryAlt: '#ebeced',
        neutralPrimary: '#d1d4d5',
        neutralDark: '#f4f5f6',
        black: '#fafafa',
        white: '#202c33',
    }
});

loadTheme(myTheme);
