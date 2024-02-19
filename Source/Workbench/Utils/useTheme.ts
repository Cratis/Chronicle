// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useDarkMode } from "usehooks-ts";
import { useLayoutEffect } from "react";

export const useTheme = () => {
    const { isDarkMode, toggle, enable, disable } = useDarkMode();

    useLayoutEffect(() => {
        const theme = isDarkMode ? 'dark' : 'light';
        document.getElementById('theme')?.setAttribute('href', `/themes/${theme}.css`);
        document.body.classList.toggle('dark', isDarkMode);
        document.body.classList.toggle('light', !isDarkMode);
    }, [isDarkMode]);
    return { isDarkMode, toggle, enable, disable }
}
