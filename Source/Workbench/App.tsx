// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useTheme } from './Utils/useTheme';
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { BlankLayout } from "./Layout/Blank/BlankLayout";
import { Home } from "./Features/Home";
import { EventStore } from "./Features/EventStore/EventStore";

function App() {
    useTheme();
    return (
        <BrowserRouter>
            <Routes>
                <Route path='/' element={<Navigate to={'/home'} />} />
                <Route path='/home' element={<BlankLayout />}>
                    <Route path={''} element={<Home />} />
                </Route>
                <Route path='/event-store/*' element={<EventStore />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
