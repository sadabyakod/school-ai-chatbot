-- Sample Questions for Exam System Testing
-- Run this script to populate the database with test questions

USE [school-ai-chatbot];
GO

-- Insert sample questions for Mathematics/Algebra
INSERT INTO Questions (Subject, Chapter, Topic, Text, Explanation, Difficulty, Type, CreatedAt) VALUES
('Mathematics', 'Algebra', 'Basic Operations', 'What is 2 + 2?', 'Basic addition of two numbers.', 'Easy', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Linear Equations', 'Solve for x: 3x + 5 = 14', 'Subtract 5 from both sides, then divide by 3. Answer: x = 3', 'Medium', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Calculus', 'Find the derivative of f(x) = x² + 3x + 2', 'Use power rule: d/dx(x²) = 2x, d/dx(3x) = 3. Answer: 2x + 3', 'Hard', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Basic Operations', 'What is 5 × 3?', 'Multiplication of two numbers.', 'Easy', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Quadratic Equations', 'Which is a solution to x² - 5x + 6 = 0?', 'Factor: (x-2)(x-3) = 0. Solutions: x = 2 or x = 3', 'Medium', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Functions', 'What is the domain of f(x) = 1/(x-2)?', 'Function undefined when denominator is zero. x ≠ 2', 'Hard', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Basic Operations', 'What is 10 - 7?', 'Simple subtraction.', 'Easy', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Linear Equations', 'Solve: 2x - 4 = 10', 'Add 4 to both sides, then divide by 2. Answer: x = 7', 'Medium', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Exponents', 'Simplify: (2x³)²', 'Use power rules: (ab)ⁿ = aⁿbⁿ and (xᵐ)ⁿ = xᵐⁿ. Answer: 4x⁶', 'Hard', 'MultipleChoice', GETDATE()),
('Mathematics', 'Algebra', 'Basic Operations', 'What is 8 ÷ 2?', 'Simple division.', 'Easy', 'MultipleChoice', GETDATE());

-- Get the question IDs that were just inserted
DECLARE @Q1_ID INT = (SELECT Id FROM Questions WHERE Text = 'What is 2 + 2?');
DECLARE @Q2_ID INT = (SELECT Id FROM Questions WHERE Text = 'Solve for x: 3x + 5 = 14');
DECLARE @Q3_ID INT = (SELECT Id FROM Questions WHERE Text LIKE 'Find the derivative%');
DECLARE @Q4_ID INT = (SELECT Id FROM Questions WHERE Text = 'What is 5 × 3?');
DECLARE @Q5_ID INT = (SELECT Id FROM Questions WHERE Text LIKE 'Which is a solution to x%');
DECLARE @Q6_ID INT = (SELECT Id FROM Questions WHERE Text LIKE 'What is the domain%');
DECLARE @Q7_ID INT = (SELECT Id FROM Questions WHERE Text = 'What is 10 - 7?');
DECLARE @Q8_ID INT = (SELECT Id FROM Questions WHERE Text = 'Solve: 2x - 4 = 10');
DECLARE @Q9_ID INT = (SELECT Id FROM Questions WHERE Text LIKE 'Simplify: (2x³)²%');
DECLARE @Q10_ID INT = (SELECT Id FROM Questions WHERE Text = 'What is 8 ÷ 2?');

-- Insert options for Question 1 (Easy: 2 + 2)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q1_ID, '3', 0),
(@Q1_ID, '4', 1),
(@Q1_ID, '5', 0),
(@Q1_ID, '2', 0);

-- Insert options for Question 2 (Medium: 3x + 5 = 14)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q2_ID, 'x = 2', 0),
(@Q2_ID, 'x = 3', 1),
(@Q2_ID, 'x = 4', 0),
(@Q2_ID, 'x = 5', 0);

-- Insert options for Question 3 (Hard: Derivative)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q3_ID, '2x', 0),
(@Q3_ID, '2x + 3', 1),
(@Q3_ID, 'x² + 3', 0),
(@Q3_ID, '3x + 2', 0);

-- Insert options for Question 4 (Easy: 5 × 3)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q4_ID, '8', 0),
(@Q4_ID, '15', 1),
(@Q4_ID, '12', 0),
(@Q4_ID, '18', 0);

-- Insert options for Question 5 (Medium: Quadratic)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q5_ID, 'x = 1', 0),
(@Q5_ID, 'x = 2', 1),
(@Q5_ID, 'x = 4', 0),
(@Q5_ID, 'x = 5', 0);

-- Insert options for Question 6 (Hard: Domain)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q6_ID, 'All real numbers', 0),
(@Q6_ID, 'All real numbers except x = 2', 1),
(@Q6_ID, 'x > 2', 0),
(@Q6_ID, 'x < 2', 0);

-- Insert options for Question 7 (Easy: 10 - 7)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q7_ID, '2', 0),
(@Q7_ID, '3', 1),
(@Q7_ID, '4', 0),
(@Q7_ID, '5', 0);

-- Insert options for Question 8 (Medium: 2x - 4 = 10)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q8_ID, 'x = 3', 0),
(@Q8_ID, 'x = 7', 1),
(@Q8_ID, 'x = 5', 0),
(@Q8_ID, 'x = 6', 0);

-- Insert options for Question 9 (Hard: Exponents)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q9_ID, '2x⁵', 0),
(@Q9_ID, '4x⁶', 1),
(@Q9_ID, '4x⁵', 0),
(@Q9_ID, '2x⁶', 0);

-- Insert options for Question 10 (Easy: 8 ÷ 2)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
(@Q10_ID, '3', 0),
(@Q10_ID, '4', 1),
(@Q10_ID, '5', 0),
(@Q10_ID, '6', 0);

-- Verify insertion
SELECT 
    q.Id,
    q.Subject,
    q.Chapter,
    q.Difficulty,
    q.Text,
    COUNT(qo.Id) as OptionCount
FROM Questions q
LEFT JOIN QuestionOptions qo ON q.Id = qo.QuestionId
WHERE q.Subject = 'Mathematics' AND q.Chapter = 'Algebra'
GROUP BY q.Id, q.Subject, q.Chapter, q.Difficulty, q.Text
ORDER BY 
    CASE q.Difficulty 
        WHEN 'Easy' THEN 1 
        WHEN 'Medium' THEN 2 
        WHEN 'Hard' THEN 3 
    END;

PRINT '✅ Successfully inserted 10 questions with 40 options';
PRINT '   - 4 Easy questions';
PRINT '   - 3 Medium questions';
PRINT '   - 3 Hard questions';
