-- =============================================
-- Seed Exam Template and Sample Questions
-- Script to populate ExamTemplates and Questions tables
-- =============================================

USE [school-ai-chatbot];
GO

-- =============================================
-- 1. Seed Exam Template
-- =============================================
IF NOT EXISTS (SELECT 1 FROM ExamTemplates WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT ExamTemplates ON;
    
    INSERT INTO ExamTemplates (Id, Name, Subject, Chapter, TotalQuestions, DurationMinutes, AdaptiveEnabled, CreatedAt, CreatedBy)
    VALUES (
        1,
        N'Demo Math Test',
        N'Mathematics',
        N'Integers',
        5,
        10,
        1,
        GETDATE(),
        N'System'
    );
    
    SET IDENTITY_INSERT ExamTemplates OFF;
    
    PRINT '✅ Inserted ExamTemplate: Demo Math Test (Id=1)';
END
ELSE
BEGIN
    PRINT 'ℹ️  ExamTemplate with Id=1 already exists';
END
GO

-- =============================================
-- 2. Check and Seed Questions for Mathematics/Integers
-- =============================================

-- Count existing questions
DECLARE @QuestionCount INT;
SELECT @QuestionCount = COUNT(*) 
FROM Questions 
WHERE Subject = N'Mathematics' AND Chapter = N'Integers';

PRINT '📊 Current question count for Mathematics/Integers: ' + CAST(@QuestionCount AS NVARCHAR(10));

-- Only insert if less than 5 questions exist
IF @QuestionCount < 5
BEGIN
    PRINT '📝 Inserting sample questions for Mathematics/Integers...';
    
    -- Insert 5 sample questions with varied difficulty
    INSERT INTO Questions (Subject, Chapter, Topic, Text, Explanation, Difficulty, Type, CreatedAt)
    VALUES
    -- Easy Question 1
    (N'Mathematics', N'Integers', N'Addition', 
     N'What is the sum of 15 + 8?', 
     N'Add the two integers: 15 + 8 = 23', 
     N'Easy', N'MultipleChoice', GETDATE()),
    
    -- Easy Question 2
    (N'Mathematics', N'Integers', N'Subtraction', 
     N'What is 20 - 7?', 
     N'Subtract the second number from the first: 20 - 7 = 13', 
     N'Easy', N'MultipleChoice', GETDATE()),
    
    -- Medium Question 1
    (N'Mathematics', N'Integers', N'Multiplication', 
     N'What is (-3) × 4?', 
     N'Multiply the integers. Negative × Positive = Negative. Result: -12', 
     N'Medium', N'MultipleChoice', GETDATE()),
    
    -- Medium Question 2
    (N'Mathematics', N'Integers', N'Division', 
     N'What is (-24) ÷ (-6)?', 
     N'Divide the integers. Negative ÷ Negative = Positive. Result: 4', 
     N'Medium', N'MultipleChoice', GETDATE()),
    
    -- Hard Question 1
    (N'Mathematics', N'Integers', N'Operations', 
     N'Simplify: (-5) + 3 × (-2) - 4', 
     N'Follow order of operations (PEMDAS): 3 × (-2) = -6, then (-5) + (-6) - 4 = -15', 
     N'Hard', N'MultipleChoice', GETDATE());
    
    PRINT '✅ Inserted 5 sample questions';
END
ELSE
BEGIN
    PRINT 'ℹ️  Sufficient questions already exist';
END
GO

-- =============================================
-- 3. Insert Question Options
-- =============================================

-- Get the IDs of the questions we just inserted (or existing ones)
DECLARE @Q1_ID INT = (SELECT TOP 1 Id FROM Questions WHERE Subject = N'Mathematics' AND Chapter = N'Integers' AND Text LIKE N'%sum of 15 + 8%');
DECLARE @Q2_ID INT = (SELECT TOP 1 Id FROM Questions WHERE Subject = N'Mathematics' AND Chapter = N'Integers' AND Text LIKE N'%20 - 7%');
DECLARE @Q3_ID INT = (SELECT TOP 1 Id FROM Questions WHERE Subject = N'Mathematics' AND Chapter = N'Integers' AND Text LIKE N'%(-3) × 4%');
DECLARE @Q4_ID INT = (SELECT TOP 1 Id FROM Questions WHERE Subject = N'Mathematics' AND Chapter = N'Integers' AND Text LIKE N'%(-24) ÷ (-6)%');
DECLARE @Q5_ID INT = (SELECT TOP 1 Id FROM Questions WHERE Subject = N'Mathematics' AND Chapter = N'Integers' AND Text LIKE N'%Simplify%');

-- Insert options for Question 1 (Easy: 15 + 8)
IF @Q1_ID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM QuestionOptions WHERE QuestionId = @Q1_ID)
BEGIN
    INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
    (@Q1_ID, N'22', 0),
    (@Q1_ID, N'23', 1),
    (@Q1_ID, N'24', 0),
    (@Q1_ID, N'25', 0);
    PRINT '✅ Added options for Question 1 (15 + 8)';
END

-- Insert options for Question 2 (Easy: 20 - 7)
IF @Q2_ID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM QuestionOptions WHERE QuestionId = @Q2_ID)
BEGIN
    INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
    (@Q2_ID, N'12', 0),
    (@Q2_ID, N'13', 1),
    (@Q2_ID, N'14', 0),
    (@Q2_ID, N'27', 0);
    PRINT '✅ Added options for Question 2 (20 - 7)';
END

-- Insert options for Question 3 (Medium: (-3) × 4)
IF @Q3_ID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM QuestionOptions WHERE QuestionId = @Q3_ID)
BEGIN
    INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
    (@Q3_ID, N'12', 0),
    (@Q3_ID, N'-12', 1),
    (@Q3_ID, N'-7', 0),
    (@Q3_ID, N'7', 0);
    PRINT '✅ Added options for Question 3 ((-3) × 4)';
END

-- Insert options for Question 4 (Medium: (-24) ÷ (-6))
IF @Q4_ID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM QuestionOptions WHERE QuestionId = @Q4_ID)
BEGIN
    INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
    (@Q4_ID, N'-4', 0),
    (@Q4_ID, N'4', 1),
    (@Q4_ID, N'-30', 0),
    (@Q4_ID, N'30', 0);
    PRINT '✅ Added options for Question 4 ((-24) ÷ (-6))';
END

-- Insert options for Question 5 (Hard: (-5) + 3 × (-2) - 4)
IF @Q5_ID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM QuestionOptions WHERE QuestionId = @Q5_ID)
BEGIN
    INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
    (@Q5_ID, N'-11', 0),
    (@Q5_ID, N'-15', 1),
    (@Q5_ID, N'-13', 0),
    (@Q5_ID, N'-7', 0);
    PRINT '✅ Added options for Question 5 (Simplify)';
END
GO

-- =============================================
-- 4. Verification Query
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION REPORT';
PRINT '========================================';

-- Verify ExamTemplate
SELECT 
    '✅ ExamTemplate Created' AS Status,
    Id, 
    Name, 
    Subject, 
    Chapter, 
    TotalQuestions, 
    DurationMinutes, 
    AdaptiveEnabled,
    CreatedAt
FROM ExamTemplates 
WHERE Id = 1;

-- Verify Questions
PRINT '';
PRINT '📋 Questions for Mathematics/Integers:';
SELECT 
    q.Id,
    q.Difficulty,
    q.Topic,
    q.Text,
    COUNT(qo.Id) AS OptionCount
FROM Questions q
LEFT JOIN QuestionOptions qo ON q.Id = qo.QuestionId
WHERE q.Subject = N'Mathematics' AND q.Chapter = N'Integers'
GROUP BY q.Id, q.Difficulty, q.Topic, q.Text
ORDER BY 
    CASE q.Difficulty 
        WHEN 'Easy' THEN 1 
        WHEN 'Medium' THEN 2 
        WHEN 'Hard' THEN 3 
    END,
    q.Id;

-- Summary count
DECLARE @FinalCount INT;
SELECT @FinalCount = COUNT(*) 
FROM Questions 
WHERE Subject = N'Mathematics' AND Chapter = N'Integers';

PRINT '';
PRINT '========================================';
PRINT '✅ SEED COMPLETE';
PRINT '📊 Total Questions: ' + CAST(@FinalCount AS NVARCHAR(10));
PRINT '🎯 Ready for testing!';
PRINT '========================================';
GO
