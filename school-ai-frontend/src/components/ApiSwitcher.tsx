import { useState, useEffect } from 'react';
import { apiConfig } from '../config';
import type { Environment } from '../config';

export default function ApiSwitcher() {
  const [currentEnv, setCurrentEnv] = useState<Environment>(apiConfig.getCurrentEnvironment());
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    setCurrentEnv(apiConfig.getCurrentEnvironment());
  }, []);

  const handleSwitch = (env: Environment) => {
    apiConfig.setEnvironment(env);
    setCurrentEnv(env);
  };

  return (
    <div className="fixed bottom-4 right-4 z-50">
      {/* Toggle Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="bg-gradient-to-r from-purple-600 to-pink-600 text-white px-4 py-2 rounded-lg shadow-lg hover:shadow-xl transition-all duration-200 flex items-center gap-2"
        title="Switch API Environment"
      >
        <span className="text-sm font-medium">
          API: {currentEnv === 'local' ? '🏠 Local' : '☁️ Production'}
        </span>
        <svg
          className={`w-4 h-4 transition-transform ${isOpen ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute bottom-14 right-0 bg-white dark:bg-gray-800 rounded-lg shadow-2xl border border-gray-200 dark:border-gray-700 overflow-hidden min-w-[200px]">
          <div className="p-2 bg-gray-50 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
            <p className="text-xs font-semibold text-gray-600 dark:text-gray-400 uppercase">
              Select API Environment
            </p>
          </div>
          
          <button
            onClick={() => handleSwitch('local')}
            className={`w-full px-4 py-3 text-left hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors flex items-center gap-3 ${
              currentEnv === 'local' ? 'bg-purple-50 dark:bg-purple-900/20' : ''
            }`}
          >
            <span className="text-2xl">🏠</span>
            <div>
              <div className="font-medium text-gray-900 dark:text-white">Local</div>
              <div className="text-xs text-gray-500 dark:text-gray-400">localhost:8080</div>
            </div>
            {currentEnv === 'local' && (
              <svg className="w-5 h-5 ml-auto text-purple-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            )}
          </button>

          <button
            onClick={() => handleSwitch('production')}
            className={`w-full px-4 py-3 text-left hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors flex items-center gap-3 ${
              currentEnv === 'production' ? 'bg-purple-50 dark:bg-purple-900/20' : ''
            }`}
          >
            <span className="text-2xl">☁️</span>
            <div>
              <div className="font-medium text-gray-900 dark:text-white">Production</div>
              <div className="text-xs text-gray-500 dark:text-gray-400">Azure App Service</div>
            </div>
            {currentEnv === 'production' && (
              <svg className="w-5 h-5 ml-auto text-purple-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            )}
          </button>
        </div>
      )}
    </div>
  );
}
