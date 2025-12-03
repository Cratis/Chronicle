// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter, Route, Routes } from "react-router-dom";
import { BlankLayout } from "./Layout/Blank/BlankLayout";
import { Home } from "./Features/Home";
import { EventStore } from "./Features/EventStore/EventStore";
import { LayoutProvider } from './Layout/Default/context/LayoutContext';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from 'Components/Dialogs';
import { Arc } from '@cratis/arc.react';
import { MVVM } from '@cratis/arc.react.mvvm';

const isDevelopment = process.env.NODE_ENV === 'development';


function App() {
    const basePathElement = document.querySelector('meta[name="base-path"]') as HTMLMetaElement;
    const basePath = basePathElement?.content ?? '/';

    return (
        <Arc development={isDevelopment} apiBasePath={basePath} basePath={basePath}>
            <MVVM>
                <LayoutProvider>
                    <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                        <BrowserRouter>
                            <Routes>
                                <Route path={basePath}>
                                    <Route path='' element={<BlankLayout />}>
                                        <Route path={''} element={<Home />} />
                                    </Route>
                                    <Route path='event-store/*' element={<EventStore />} />
                                </Route>
                            </Routes>
                        </BrowserRouter>
                    </DialogComponents>
                </LayoutProvider>
            </MVVM>
        </Arc>
    );
}

export default App;
