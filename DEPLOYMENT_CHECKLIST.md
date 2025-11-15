# ✅ Migration Deployment Checklist

Use this checklist to track your deployment progress.

---

## 📋 Pre-Deployment

### Code Review
- [x] All new models created (ChatHistory, StudyNote)
- [x] All services implemented (ChatHistory, RAG, StudyNotes)
- [x] Controllers updated (Chat with history, new Notes)
- [x] Program.cs services registered
- [x] Documentation created
- [ ] Code reviewed and tested locally

### Environment Setup
- [ ] OpenAI API key configured in appsettings.json
- [ ] Pinecone credentials configured
- [ ] Azure SQL connection string configured
- [ ] All secrets in Azure App Settings (not in code)

---

## 🗄️ Database Migration

### Local Database
- [ ] Run: `dotnet ef migrations add AddChatHistoryAndStudyNotes`
- [ ] Run: `dotnet ef database update`
- [ ] Verify ChatHistories table exists
- [ ] Verify StudyNotes table exists
- [ ] Verify indexes created (IX_ChatHistory_UserSession, IX_StudyNote_UserCreated)

### Production Database
- [ ] Backup production database
- [ ] Run migration on production: `dotnet ef database update --connection "..."`
- [ ] Verify tables created in Azure SQL
- [ ] Test database connectivity from backend

---

## 🧪 Local Testing

### Backend Testing
- [ ] Run: `.\start-backend.ps1` (or `dotnet run`)
- [ ] Backend starts without errors
- [ ] GET http://localhost:8080/api/chat/test returns "✅ Chat endpoint is working!"
- [ ] GET http://localhost:8080/api/notes/test returns "✅ Notes endpoint is working!"

### Endpoint Testing
- [ ] Run: `.\test-endpoints.ps1`
- [ ] Chat endpoint responds correctly
- [ ] Chat history saves to database
- [ ] Study notes generation works
- [ ] Study notes history retrieves correctly
- [ ] File upload works (optional test)

### Manual Testing
- [ ] Test chat with RAG context
- [ ] Test chat with sessionId continuity
- [ ] Test "explain more" follow-up
- [ ] Test study notes generation for different topics
- [ ] Test study notes rating (1-5)

---

## 🌐 Frontend Updates

### Code Changes
- [ ] Add `sessionId` state to ChatBot component
- [ ] Update chat request to include sessionId
- [ ] Save sessionId from response for next request
- [ ] (Optional) Create StudyNotes component
- [ ] (Optional) Add navigation to Study Notes page

### Testing
- [ ] Test chat with session continuity
- [ ] Test "explain more" feature
- [ ] Test study notes generation (if implemented)
- [ ] Verify no console errors

---

## 🚀 Deployment to Azure

### Backend Deployment
- [ ] Commit all changes: `git add .`
- [ ] Create commit: `git commit -m "Migrated Azure Functions to ASP.NET Core"`
- [ ] Push to GitHub: `git push origin main`
- [ ] GitHub Actions workflow starts
- [ ] Backend deployment succeeds
- [ ] Check deployment logs for errors

### Frontend Deployment
- [ ] Frontend builds successfully
- [ ] Frontend deploys to Azure Static Web Apps
- [ ] Check deployment logs

### Post-Deployment Checks
- [ ] Backend health check: https://studyai-ingestion-345.azurewebsites.net/api/chat/test
- [ ] Notes health check: https://studyai-ingestion-345.azurewebsites.net/api/notes/test
- [ ] Frontend loads correctly
- [ ] Frontend can reach backend API

---

## ✔️ Production Verification

### Chat Functionality
- [ ] Test chat endpoint in production:
  ```bash
  curl "https://studyai-ingestion-345.azurewebsites.net/api/chat?code=KEY" \
    -H "Content-Type: application/json" \
    -d '{"question": "What is photosynthesis?", "sessionId": "prod-test-1"}'
  ```
- [ ] Response includes `sessionId`
- [ ] Response includes `reply` with AI answer
- [ ] Chat history saves to database
- [ ] Subsequent requests with same sessionId maintain context

### Study Notes Functionality
- [ ] Test notes generation:
  ```bash
  curl "https://studyai-ingestion-345.azurewebsites.net/api/notes/generate?code=KEY" \
    -H "Content-Type: application/json" \
    -d '{"topic": "Gravity", "subject": "Physics", "grade": "9"}'
  ```
- [ ] Response includes markdown-formatted notes
- [ ] Notes save to database
- [ ] Notes history retrieval works
- [ ] Rating functionality works

### File Upload
- [ ] Test file upload (optional):
  ```bash
  curl "https://studyai-ingestion-345.azurewebsites.net/api/file/upload?code=KEY" \
    -F "file=@test.pdf" -F "Class=10" -F "subject=Science" -F "chapter=Ch1"
  ```
- [ ] File uploads successfully
- [ ] Chunks created and embedded
- [ ] Vectors uploaded to Pinecone
- [ ] Metadata saved to SQL

---

## 📊 Performance & Monitoring

### Database Performance
- [ ] Check query execution times in Azure Portal
- [ ] Verify indexes are being used
- [ ] Monitor database DTU usage
- [ ] Set up alerts for slow queries

### API Performance
- [ ] Test response times for /chat endpoint (target: < 3s)
- [ ] Test response times for /notes/generate (target: < 10s)
- [ ] Monitor App Service metrics
- [ ] Set up Application Insights (optional)

### Logging
- [ ] Enable Application Insights logging
- [ ] Check Azure App Service logs
- [ ] Verify error logging works
- [ ] Set up log retention policy

---

## 🔐 Security & Configuration

### Azure App Settings
- [ ] OPENAI_API_KEY configured
- [ ] Pinecone:ApiKey configured
- [ ] Pinecone:Host configured
- [ ] ConnectionStrings:DefaultConnection configured
- [ ] Jwt:Key configured
- [ ] No secrets in code or appsettings.json

### GitHub Secrets
- [ ] AZURE_FUNCTION_KEY added to GitHub secrets
- [ ] Other sensitive values added to secrets
- [ ] Workflows updated to use secrets

### CORS Configuration
- [ ] Frontend domain allowed in CORS policy
- [ ] localhost allowed for development
- [ ] Production URLs whitelisted

---

## 🗑️ Azure Functions Retirement (Optional)

### Before Retiring
- [ ] All features tested and working in ASP.NET Core
- [ ] Production traffic verified on new backend
- [ ] Backup Azure Functions code archived

### Retirement Steps
- [ ] Stop Azure Function App
- [ ] Archive `C:\SmartStudyFunc` project locally
- [ ] Remove Function App from Azure (optional)
- [ ] Update documentation to reflect retirement
- [ ] Remove Function App URLs from frontend (if any)

---

## 📚 Documentation Updates

### Project Documentation
- [ ] README.md updated with new features
- [ ] CONFIGURATION.md updated
- [ ] API documentation updated
- [ ] Deployment guide updated

### Team Communication
- [ ] Notify team of migration completion
- [ ] Share MIGRATION.md guide
- [ ] Schedule training session (if needed)
- [ ] Update project wiki/confluence

---

## 🎉 Final Verification

### User Acceptance Testing
- [ ] End-to-end chat flow works
- [ ] Study notes generation works
- [ ] File upload works
- [ ] No breaking changes for users
- [ ] Performance meets expectations

### Rollback Plan
- [ ] Know how to rollback database migration
- [ ] Keep Azure Functions running (backup)
- [ ] Have git commit to revert to
- [ ] Document rollback procedure

---

## 📈 Post-Migration Tasks

### Week 1
- [ ] Monitor error rates
- [ ] Monitor response times
- [ ] Gather user feedback
- [ ] Fix any critical issues

### Month 1
- [ ] Analyze chat history data
- [ ] Analyze study notes usage
- [ ] Optimize slow queries
- [ ] Plan future enhancements

### Long-term
- [ ] Consider adding authentication
- [ ] Add user-specific chat history UI
- [ ] Add study notes export feature
- [ ] Implement feedback collection

---

## ✅ Migration Status

**Overall Progress:** ___ / 100 items completed

### Status Legend
- [ ] Not started
- [x] Completed
- [!] Blocked/Issue

### Current Phase
- [ ] Pre-Deployment
- [ ] Database Migration
- [ ] Local Testing
- [ ] Frontend Updates
- [ ] Azure Deployment
- [ ] Production Verification
- [ ] Azure Functions Retirement
- [ ] Documentation & Training
- [x] **COMPLETED!** 🎉

---

**Last Updated:** [Date]  
**Completed By:** [Name]  
**Notes:** _Add any important notes or issues here_
