// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useDarkMode } from "usehooks-ts";
import { Button } from "primereact/button";

export const ThemeSwitch = () => {
    const { isDarkMode, toggle } = useDarkMode()
    return (
        <Button onClick={toggle} size="small" rounded>
            {isDarkMode ? 'Light' : 'Dark'}
        </Button>
    )
}
