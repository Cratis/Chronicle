// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
    palette: {
        mode: 'dark',
        primary: {
            main: 'rgb(171,171,238)',
            light: 'rgb(206,195,250)',
            dark: 'rgb(57, 48, 83)',
            contrastText: 'rgb(255, 255, 255)'
        },
        background: {
            paper: 'rgb(11,8,19)',
            default: 'rgb(0,0,0)'
        },
        text: {
            primary: 'rgb(201,201,201)',
            secondary: 'rgb(224,224,224)'
        },
        divider: 'rgb(82,82,82)'
    }
});
