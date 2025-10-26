#!/bin/bash

# Azure Static Web Apps Local Development Setup

echo "🚀 Setting up Azure Static Web Apps local development..."

# Check if Azure Functions Core Tools is installed
if ! command -v func &> /dev/null; then
    echo "❌ Azure Functions Core Tools not found. Installing..."
    npm install -g azure-functions-core-tools@4 --unsafe-perm true
else
    echo "✅ Azure Functions Core Tools found"
fi

# Check if Static Web Apps CLI is installed
if ! command -v swa &> /dev/null; then
    echo "❌ Static Web Apps CLI not found. Installing..."
    npm install -g @azure/static-web-apps-cli
else
    echo "✅ Static Web Apps CLI found"
fi

echo "🔧 Setting up environment..."

# Copy environment file for SWA
cd school-ai-frontend
cp .env.swa .env
echo "✅ Environment configured for Static Web Apps"

# Install frontend dependencies
echo "📦 Installing frontend dependencies..."
npm install

echo "🏗️  Starting local development environment..."
echo "Frontend will run on: http://localhost:4280"
echo "API will run on: http://localhost:7071"
echo ""
echo "Use Ctrl+C to stop the development server"

# Start SWA emulator (this will start both frontend and API)
cd ..
swa start school-ai-frontend --api-location api --port 4280