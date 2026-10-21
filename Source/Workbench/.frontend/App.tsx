// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter, Route, Routes } from "react-router-dom";
import { BlankLayout } from "../Layout/Blank/BlankLayout";
import { Home } from "../Features/Home";
import { EventStore } from "../Features/EventStore/EventStore";
import { Login, AuthProvider, ProtectedRoute } from "../Features/Security";
import { LayoutProvider } from '../Layout/Default/context/LayoutContext';
import { DialogComponents } from '@cratis/arc.react/dialogs';
import { BusyIndicatorDialog } from '@cratis/components/Dialogs';
import { ConfirmationDialog } from 'Components/Dialogs';
import { Arc } from '@cratis/arc.react';
import { MVVM } from '@cratis/arc.react.mvvm';
import { basePath as configuredBasePath } from '../Utils/basePath';
import { getAntiforgeryHeaders } from '../Features/Security/antiforgery';

const isDevelopment = import.meta.env.MODE === 'development';

function App() {
    // Normalized to '' at the root. The router takes it as its basename, so every route, link and navigation
    // inside the app is written without it and the router adds it - and strips it from the location it reports.
    // Only what leaves the router (fetches, full page loads, Arc's API calls) needs the prefix, and gets it
    // from Arc's apiBasePath and absolutePath().
    const basePath = configuredBasePath;

    return (
        <Arc
            development={isDevelopment}
            apiBasePath={basePath}
            basePath={basePath}
            httpHeadersCallback={getAntiforgeryHeaders}>
            <MVVM>
                <LayoutProvider>
                    <DialogComponents confirmation={ConfirmationDialog} busyIndicator={BusyIndicatorDialog}>
                        <BrowserRouter basename={basePath}>
                            <AuthProvider>
                                <Routes>
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
