// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { List, styled } from '@mui/material';

export const NavigationContainer = styled(List)<{ component?: React.ElementType }>({
    // '& .MuiListItemButton-root': {
    //     paddingLeft: 24,
    //     paddingRight: 24,
    // },
    '& .MuiListItemIcon-root': {
        minWidth: 0,
        marginRight: 16,
    },
    '& .MuiSvgIcon-root': {
        fontSize: 20,
    },
});
