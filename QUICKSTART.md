# ⚡ Quick Start Guide

Get up and running in 3 minutes!

## Step 1: Navigate to Project
```bash
cd /Users/tominjose/CascadeProjects/WindsurfProductAPI
```

## Step 2: Set API Key (Optional)
```bash
export WINDSURF_API_KEY="your_api_key_here"
```

> **Skip this step** to use mock AI data for testing!

## Step 3: Run the API
```bash
dotnet run
```

## Step 4: Open Swagger UI
Open your browser to: **https://localhost:5001**

## Step 5: Try It Out!

### Test Basic CRUD
1. Click on **GET /api/products**
2. Click "Try it out" → "Execute"
3. See the pre-seeded products!

### Test AI Features
1. Click on **POST /api/products/1/ai-insights**
2. Click "Try it out" → "Execute"
3. Watch AI generate marketing content, positioning analysis, and more!

### Test Catalog Analysis
1. Click on **POST /api/catalog/ai-insights**
2. Click "Try it out" → "Execute"
3. Get business insights about your entire catalog!

## 🎉 That's It!

You now have a fully functional AI-powered product API running locally.

## Next Steps

- Create your own products via POST /api/products
- Experiment with different AI endpoints
- Try batch analyzing your catalog
- Check out the full README.md for advanced features

## Troubleshooting

**Port already in use?**
```bash
dotnet run --urls "http://localhost:5555;https://localhost:5556"
```

**Need to restore packages?**
```bash
dotnet restore
```

**Want to see detailed logs?**
```bash
dotnet run --verbosity detailed
```
