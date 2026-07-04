import { Environment } from './environment';

/**
 * Local development configuration. Points directly at the .NET API's Kestrel dev
 * port (see src/AI.DocumentIntelligence.Api/Properties/launchSettings.json). The API's
 * CORS policy already allows http://localhost:4200, so no proxy is strictly required —
 * but it is a straightforward relative path if a proxy is preferred later.
 */
export const environment: Environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5235/api/v1',
};
