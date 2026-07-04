import { HttpContextToken } from '@angular/common/http';

/**
 * Marks a request as exempt from the auth interceptor's Bearer-token attach step and
 * from the 401 refresh-and-retry flow — used for the login/refresh calls themselves so
 * they never recurse into a refresh loop.
 */
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);
