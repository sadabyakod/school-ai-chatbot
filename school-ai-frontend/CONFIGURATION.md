# Configuration Guide

## Azure Function App Setup

This application is configured to work with an Azure Function App backend. To connect properly, you need to configure the Azure Function Key.

### Step 1: Get Your Azure Function Key

Your Azure Function endpoint is:
```
https://app-wlanqwy7vuwmu.azurewebsites.net/api
```

To get your function key:

1. **From Azure Portal:**
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to your Function App: `app-wlanqwy7vuwmu`
   - Go to **Functions** → **App keys**
   - Copy one of the function keys (default or host key)

2. **From Azure CLI:**
   ```bash
   az functionapp keys list --name app-wlanqwy7vuwmu --resource-group <your-resource-group>
   ```

3. **From deployment output:**
   - If you deployed using `azd up`, the function key should be in the deployment output

### Step 2: Configure Environment Variables

1. Copy the example environment file:
   ```bash
   cp .env.local.example .env.local
   ```

2. Edit `.env.local` and replace `YOUR_FUNCTION_KEY_HERE` with your actual function key:
   ```bash
   VITE_API_URL=https://app-wlanqwy7vuwmu.azurewebsites.net/api
   VITE_AZURE_FUNCTION_KEY=your_actual_function_key_here
   ```

### Step 3: Restart the Development Server

After updating `.env.local`, restart the development server:

```bash
npm run dev
```

## API Endpoints

Once configured, the application will call these endpoints:

- **Chat**: `https://app-wlanqwy7vuwmu.azurewebsites.net/api/chat?code=YOUR_FUNCTION_KEY`
- **File Upload**: `https://app-wlanqwy7vuwmu.azurewebsites.net/api/upload/textbook?code=YOUR_FUNCTION_KEY`
- **FAQs**: `https://app-wlanqwy7vuwmu.azurewebsites.net/api/faqs?code=YOUR_FUNCTION_KEY`
- **Analytics**: `https://app-wlanqwy7vuwmu.azurewebsites.net/api/analytics?code=YOUR_FUNCTION_KEY`

## Local Development (Optional)

If you want to run the backend locally instead:

1. Update `.env.local`:
   ```bash
   VITE_API_URL=http://localhost:7071/api
   VITE_AZURE_FUNCTION_KEY=
   ```
   
2. Start the local Azure Functions backend (requires Azure Functions Core Tools)

## Troubleshooting

### "Cannot connect to backend" error
- Verify your function key is correct in `.env.local`
- Check that the Azure Function App is running and accessible
- Ensure you've restarted the dev server after changing `.env.local`

### "401 Unauthorized" error
- Your function key may be incorrect or expired
- Regenerate the key from Azure Portal and update `.env.local`

### CORS errors
- Make sure the Azure Function App has CORS configured for your domain
- For local development, `http://localhost:5173` should be allowed
