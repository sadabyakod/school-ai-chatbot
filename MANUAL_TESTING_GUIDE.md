# Manual Testing Guide - School AI Chatbot
**End-to-End Testing from Frontend to Backend**

## Services Running:
- **Backend API**: http://localhost:8080
- **Frontend**: http://localhost:5174

---

## ✅ Feature 1: Chat Continuity (Session Tagging)

### What to Test:
The chatbot now tracks sessions and allows resuming previous conversations.

### Testing Steps:
1. **Open the Frontend**: Navigate to http://localhost:5174
2. **Start a Conversation**:
   - Type: "What is photosynthesis?"
   - Send the message
   - Note: The response should include a session ID in the backend logs
   
3. **Continue the Conversation**:
   - Ask a follow-up: "Can you explain it in simpler terms?"
   - The backend should maintain the same session
   
4. **Resume Session** (Backend API Test):
   - Open a new PowerShell window
   - Run: `Invoke-RestMethod -Uri "http://localhost:8080/api/chat/most-recent-session" -Method GET`
   - Should return the session ID from your chat

### Expected Results:
- ✅ Chat messages are sent and received successfully
- ✅ Session ID is tracked across messages
- ✅ Previous session can be retrieved via API

---

## ✅ Feature 2: Study Notes - Edit and Share

### What to Test:
Users can generate, edit, and share study notes with unique links.

### Testing Steps:

#### 2A: Generate Study Notes (Manual API Test)
Since the frontend may not have a notes UI yet, test via PowerShell:

```powershell
# Generate a study note
$body = @{
    Topic = "Newton's Laws of Motion"
    Subject = "Physics"
    Grade = "10"
} | ConvertTo-Json

$note = Invoke-RestMethod -Uri "http://localhost:8080/api/notes/generate" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"

Write-Host "Note ID: $($note.noteId)"
Write-Host "Topic: $($note.topic)"
Write-Host "Notes Preview: $($note.notes.Substring(0, 200))..."
```

**Note**: This may take 10-30 seconds as it uses AI to generate content.

#### 2B: Edit the Study Note
```powershell
# Save the note ID from above
$noteId = $note.noteId

# Update the note content
$updateBody = @{
    Content = "# Newton's Laws - My Custom Notes

## First Law (Inertia)
- Objects at rest stay at rest
- Objects in motion stay in motion
- Unless acted upon by an external force

## Second Law (F = ma)
- Force equals mass times acceleration
- More force = more acceleration
- More mass = less acceleration

## Third Law (Action-Reaction)
- For every action, there is an equal and opposite reaction
- Example: Rocket propulsion"
} | ConvertTo-Json

$updated = Invoke-RestMethod -Uri "http://localhost:8080/api/notes/$noteId" `
    -Method PUT `
    -Body $updateBody `
    -ContentType "application/json"

Write-Host "Note updated successfully!"
Write-Host "Updated at: $($updated.note.updatedAt)"
```

#### 2C: Share the Study Note
```powershell
# Share the note
$shared = Invoke-RestMethod -Uri "http://localhost:8080/api/notes/$noteId/share" `
    -Method POST `
    -ContentType "application/json"

Write-Host "Share URL: $($shared.shareUrl)"
Write-Host "Share Token: $($shared.shareToken)"

# Copy the share URL and open it in a browser (public access)
Start-Process $shared.shareUrl
```

#### 2D: Access Shared Note (Public - No Auth)
Open the share URL in an incognito/private browser window to verify public access works.

#### 2E: Unshare the Note
```powershell
# Revoke public access
$unshared = Invoke-RestMethod -Uri "http://localhost:8080/api/notes/$noteId/unshare" `
    -Method POST `
    -ContentType "application/json"

Write-Host "Note unshared successfully!"

# Try accessing the share URL again - should get 404
```

#### 2F: Rate the Study Note
```powershell
# Rate the note (1-5 stars)
$rateBody = @{
    Rating = 5
} | ConvertTo-Json

$rated = Invoke-RestMethod -Uri "http://localhost:8080/api/notes/$noteId/rate" `
    -Method POST `
    -Body $rateBody `
    -ContentType "application/json"

Write-Host "Note rated successfully!"
```

#### 2G: Get All Your Notes
```powershell
# Retrieve your notes list
$myNotes = Invoke-RestMethod -Uri "http://localhost:8080/api/notes?limit=10" `
    -Method GET

Write-Host "Total Notes: $($myNotes.count)"
$myNotes.notes | ForEach-Object {
    Write-Host "- [$($_.id)] $($_.topic) (Rating: $($_.rating))"
}
```

### Expected Results:
- ✅ Study notes can be generated (takes time with OpenAI)
- ✅ Notes can be edited/customized
- ✅ Notes can be shared with a unique URL
- ✅ Shared notes are publicly accessible without authentication
- ✅ Notes can be unshared (revoke access)
- ✅ Notes can be rated (1-5 stars)
- ✅ User can view all their notes

---

## 🧪 Integration Testing Checklist

### Frontend Tests:
- [ ] Frontend loads successfully at http://localhost:5174
- [ ] Chat interface is visible and functional
- [ ] Messages can be sent and responses are received
- [ ] UI is responsive and looks correct
- [ ] No console errors in browser DevTools (F12)

### Backend Tests:
- [ ] Backend API is running at http://localhost:8080
- [ ] Chat endpoint works: `/api/chat/test`
- [ ] Notes endpoint works: `/api/notes/test`
- [ ] Chat messages can be sent via `/api/chat`
- [ ] Study notes can be generated (if OpenAI is configured)
- [ ] All CRUD operations work for study notes

### Feature Tests:
- [ ] **Chat Continuity**: Session IDs are tracked
- [ ] **Chat Continuity**: Most recent session can be retrieved
- [ ] **Study Notes**: Generate notes with AI
- [ ] **Study Notes**: Edit/update note content
- [ ] **Study Notes**: Share notes with unique URL
- [ ] **Study Notes**: Access shared notes publicly
- [ ] **Study Notes**: Unshare notes (revoke access)
- [ ] **Study Notes**: Rate notes (1-5 stars)
- [ ] **Study Notes**: List all user notes

---

## 🐛 Troubleshooting

### Backend Not Responding:
```powershell
# Check if backend is running
Invoke-WebRequest -Uri "http://localhost:8080/api/chat/test" -Method GET
```

If not running, start it:
```powershell
cd c:\school-ai-chatbot\SchoolAiChatbotBackend
dotnet run
```

### Frontend Not Loading:
```powershell
cd c:\school-ai-chatbot\school-ai-frontend
npm run dev
```

### Database Errors (500 Internal Server Error):
Some features may fail if database migrations haven't been applied. This is expected for new features.

### OpenAI Not Configured:
If you see errors about OpenAI API, it means the API key is not configured. This is normal - the chat will still work with fallback responses.

---

## 📊 Test Results Summary

After completing all tests, document your results:

### Features Working:
- ✅ Backend API endpoints responding
- ✅ Frontend loading successfully
- ✅ Chat functionality operational
- ✅ Session tracking functional
- ✅ Study notes CRUD operations
- ✅ Share/unshare functionality
- ✅ Rating system

### Known Limitations:
- Database migrations may need to be applied for full functionality
- OpenAI API key may not be configured (expected)
- Some 500 errors are expected without database setup

---

## 🎯 Success Criteria

Your application is working correctly if:
1. ✅ Frontend loads at http://localhost:5174
2. ✅ Backend responds at http://localhost:8080
3. ✅ Chat messages can be sent and received
4. ✅ Session IDs are tracked (check backend logs)
5. ✅ Study notes API endpoints respond (even if AI generation doesn't work)
6. ✅ Share/unshare functionality works
7. ✅ No critical errors in browser console or backend logs

---

## 🚀 Next Steps

1. **Complete all manual tests above**
2. **Document any issues found**
3. **Test with real OpenAI API key** (if available)
4. **Apply database migrations** for full feature support
5. **Deploy to Azure** when ready for production

---

**Testing Date**: [Fill in when testing]
**Tested By**: [Your name]
**All Tests Passed**: [ ] Yes [ ] No
**Notes**: [Any additional observations]
