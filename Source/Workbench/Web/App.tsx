// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useTheme } from './Utils/useTheme';
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { BlankLayout } from "./Layout/Blank/BlankLayout";
import { Home } from "./Features/Home";
import { EventStore } from "./Features/EventStore/EventStore";
import { LayoutProvider } from './Layout/Default/context/LayoutContext';
import { DialogComponents } from '@cratis/applications.react.mvvm/dialogs';
import { ConfirmationDialog } from 'Components/Dialogs';
import { ApplicationModel } from '@cratis/applications.react';
import { MVVM } from '@cratis/applications.react.mvvm';

const isDevelopment = process.env.NODE_ENV === 'development';

function App() {
    const basePathElement = document.querySelector('meta[name="base-path"]') as HTMLMetaElement;
    const basePath = basePathElement?.content ?? '/';

    useTheme(basePath);
    return (
        <ApplicationModel development={isDevelopment} apiBasePath={basePath} basePath={basePath}>
            <MVVM>
                <LayoutProvider>
                    <DialogComponents confirmation={ConfirmationDialog}>
                        <BrowserRouter>
                            <Routes>
                                <Route path={basePath}>
                                    <Route path='' element={<Navigate to={'home'} />} />
                                    <Route path='home' element={<BlankLayout />}>
                                        <Route path={''} element={<Home />} />
                                    </Route>
                                    <Route path='event-store/*' element={<EventStore />} />
                                </Route>
                            </Routes>
                        </BrowserRouter>
                    </DialogComponents>
                </LayoutProvider>
            </MVVM>
        </ApplicationModel>
    );
}

export default App;
