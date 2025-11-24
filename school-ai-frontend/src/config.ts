// API Configuration Manager
// Allows switching between local and production API from Azure frontend

export type Environment = 'local' | 'production';

// Get saved environment preference from localStorage
const getSavedEnvironment = (): Environment => {
  const saved = localStorage.getItem('api-environment');
  return (saved === 'local' || saved === 'production') ? saved : 'production';
};

// API endpoints
const API_ENDPOINTS = {
  local: 'http://localhost:8080',
  production: import.meta.env.VITE_API_URL || 'https://app-wlanqwy7vuwmu-hpbwbfgqbybqg7dp.centralindia-01.azurewebsites.net'
};

class ApiConfig {
  private environment: Environment;

  constructor() {
    this.environment = getSavedEnvironment();
  }

  getApiUrl(): string {
    return API_ENDPOINTS[this.environment];
  }

  getCurrentEnvironment(): Environment {
    return this.environment;
  }

  setEnvironment(env: Environment): void {
    this.environment = env;
    localStorage.setItem('api-environment', env);
    // Reload to apply changes
    window.location.reload();
  }

  isLocal(): boolean {
    return this.environment === 'local';
  }

  isProduction(): boolean {
    return this.environment === 'production';
  }
}

export const apiConfig = new ApiConfig();
