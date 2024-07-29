// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Outlet } from "react-router-dom";

export const BlankLayout = () => {
    return (
        <>
            <div>
                <Outlet/>
            </div>
        </>
    );
};
