import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

// Note: no BrowserAnimationsModule/provideAnimationsAsync — @angular/animations is not
// installed in this workspace, so Material components with internal animation triggers
// (MatMenu, MatSnackBar) fall back to Angular's no-op animation driver (instant, no
// transition, with a one-time console notice). All custom "subtle animation" polish
// (landing page, etc.) uses plain CSS/SCSS `@keyframes`, which is unaffected by this.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
  ]
};
