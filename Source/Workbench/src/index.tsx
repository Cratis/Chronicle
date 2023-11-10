import { PrimeReactProvider } from 'primereact/api';
import ReactDOM from 'react-dom/client';
import './Styles/tailwind.css';
import './Styles/theme.css';
import React from 'react';
import App from "./App";


ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider value={{ ripple: true }}>
            <App/>
        </PrimeReactProvider>
    </React.StrictMode>
);
