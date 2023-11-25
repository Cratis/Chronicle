// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useDarkMode } from "usehooks-ts";
import { useLayoutEffect } from "react";
import { switchThemeCss } from "./switchTheme";

export const useColorScheme = () => {
    const { isDarkMode, toggle, enable, disable } = useDarkMode();

    useLayoutEffect(() => {
        document.body.classList.toggle('dark', isDarkMode);
        document.body.classList.toggle('light', !isDarkMode);
        const newTheme = isDarkMode ? 'dark' : 'light';
        const oldTheme = isDarkMode ? 'light' : 'dark';
        switchThemeCss(newTheme, oldTheme, 'theme');
    }, [isDarkMode]);
    return { isDarkMode, toggle, enable, disable }
}
