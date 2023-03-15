// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: 'rgb(68, 60, 104)',
            light: 'rgb(99, 89, 133)',
            dark: 'rgb(57, 48, 83)',
            contrastText: 'rgb(255, 255, 255)'
        },
        background: {
            paper: 'rgb(11,8,19)',
            default: 'rgb(24, 18, 43)'
        },
        text: {
            primary: 'rgb(201,201,201)',
            secondary: 'rgb(129,129,129)'
        },
        divider: 'rgb(82,82,82)'

    }
});
