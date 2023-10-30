import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { PrimeReactProvider } from 'primereact/api';
import { appRoutes } from './Routing/appRoutes';
// import { Layout } from './Layout/Layout';
import ReactDOM from 'react-dom/client';
import './Styles/tailwind.css';
import './Styles/theme.css';
import React from 'react';

const routes = createBrowserRouter(appRoutes);

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider value={{}}>
            {/* <Layout> */}
                <RouterProvider router={routes} />
            {/* </Layout> */}
        </PrimeReactProvider>
    </React.StrictMode>
);
