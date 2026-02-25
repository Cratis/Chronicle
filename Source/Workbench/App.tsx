// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter, Route, Routes } from "react-router-dom";
import { BlankLayout } from "./Layout/Blank/BlankLayout";
import { Home } from "./Features/Home";
import { EventStore } from "./Features/EventStore/EventStore";
import { Login, AuthProvider, ProtectedRoute } from "./Features/Security";
import { LayoutProvider } from './Layout/Default/context/LayoutContext';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog, ConfirmationDialog } from '@cratis/components/Dialogs';
import { Arc } from '@cratis/arc.react';
import { MVVM } from '@cratis/arc.react.mvvm';

const isDevelopment = import.meta.env.MODE === 'development';

function App() {
    const basePathElement = document.querySelector('meta[name="base-path"]') as HTMLMetaElement;
    const basePath = basePathElement?.content ?? '/';

    return (
        <Arc development={isDevelopment} apiBasePath={basePath} basePath={basePath}>
            <MVVM>
                <LayoutProvider>
                    <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                        <BrowserRouter>
                            <AuthProvider>
                                <Routes>
                                    <Route path={basePath}>
                                        <Route path='login' element={<BlankLayout />}>
                                            <Route path='' element={<Login />} />
                                        </Route>
                                        <Route path='' element={<BlankLayout />}>
                                            <Route path='' element={
                                                <ProtectedRoute>
                                                    <Home />
                                                </ProtectedRoute>
                                            } />
                                        </Route>
                                        <Route path='event-store/*' element={
                                            <ProtectedRoute>
                                                <EventStore />
                                            </ProtectedRoute>
                                        } />
                                    </Route>
                                </Routes>
                            </AuthProvider>
                        </BrowserRouter>
                    </DialogComponents>
                </LayoutProvider>
            </MVVM>
        </Arc>
    );
}

export default App;
