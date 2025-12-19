# MCQ Answers Storage in WrittenSubmissions

## Overview
Students can now submit MCQ answers along with their answer sheet upload. These answers are stored in JSON format in the `WrittenSubmissions` table.

## Changes Made

### 1. Database Schema
- **Added Column**: `McqAnswers` (NVARCHAR(MAX)) to `WrittenSubmissions` table
- **Migration File**: `add-mcq-answers-to-written-submissions.sql`

### 2. Model Changes (`WrittenSubmission.cs`)
- Added `McqAnswersJson` property (mapped to database column)
- Added `McqAnswers` property (in-memory list for easy access)
- Added `McqAnswerDto` class for type-safe MCQ answer representation

```csharp
public class McqAnswerDto
{
    public string QuestionId { get; set; }
    public string SelectedOption { get; set; }
}
```

### 3. API Changes (`ExamSubmissionController.cs`)
- Modified `POST /api/exam/upload-written` endpoint
- Added optional `mcqAnswers` form parameter
- Parses and validates JSON before storing

## Usage

### From Mobile App

```typescript
const formData = new FormData();
formData.append('examId', examId);
formData.append('studentId', studentId);
formData.append('files', answerSheetImage);

// Add MCQ answers as JSON string
const mcqAnswers = [
  { questionId: "Q1", selectedOption: "A" },
  { questionId: "Q2", selectedOption: "B" },
  { questionId: "Q3", selectedOption: "D" }
];
formData.append('mcqAnswers', JSON.stringify(mcqAnswers));

const response = await axios.post('/api/exam/upload-written', formData);
```

### From Postman

```
POST http://localhost:8080/api/exam/upload-written
Content-Type: multipart/form-data

Fields:
- examId: "exam_123"
- studentId: "student_456"
- files: [answer_sheet.jpg]
- mcqAnswers: [{"questionId":"Q1","selectedOption":"A"},{"questionId":"Q2","selectedOption":"B"}]
```

### JSON Format
```json
[
  {
    "questionId": "Q1",
    "selectedOption": "A"
  },
  {
    "questionId": "Q2",
    "selectedOption": "B"
  },
  {
    "questionId": "Q3",
    "selectedOption": "D"
  }
]
```

## Features

1. **Optional Parameter**: MCQ answers are optional - endpoint works with or without them
2. **Error Handling**: If JSON parsing fails, the request continues without MCQ answers
3. **Logging**: Full logging of parsed MCQ answers for debugging
4. **Type Safety**: Uses strongly-typed `McqAnswerDto` class

## Database Migration

Run the migration script:

```bash
# Using sqlcmd
sqlcmd -S smartstudysqlsrv.database.windows.net -d smartstudydb -U schooladmin -P India@12345 -i add-mcq-answers-to-written-submissions.sql

# Or using PowerShell script
.\apply-migrations.ps1
```

## Retrieving MCQ Answers

```csharp
// In your code
var submission = await _examRepository.GetWrittenSubmissionAsync(submissionId);

if (submission?.McqAnswers != null)
{
    foreach (var answer in submission.McqAnswers)
    {
        Console.WriteLine($"Q: {answer.QuestionId}, Answer: {answer.SelectedOption}");
    }
}
```

## Benefits

1. **Single Submission**: Students submit both MCQ answers and written answers together
2. **Evaluation Context**: Azure Functions can use MCQ answers during subjective evaluation
3. **Combined Results**: Easy to generate complete exam results (MCQ + Subjective)
4. **Blob Storage**: MCQ results also saved to `evaluation-results` blob for consistency

## Next Steps

1. Run the migration script to add the column
2. Test the API endpoint with MCQ answers
3. Update mobile app to send MCQ answers with answer sheet upload
4. Modify Azure Functions to utilize MCQ answers during evaluation
