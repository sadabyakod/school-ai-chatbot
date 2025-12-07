-- Insert sample questions for exam system

INSERT INTO Questions (Subject, Chapter, Topic, Text, Type, Difficulty) VALUES
('Mathematics', 'Algebra', 'Linear Equations', 'What is the value of x in the equation 2x + 5 = 15?', 'MCQ', 'Easy'),
('Mathematics', 'Algebra', 'Linear Equations', 'Solve for y: 3y - 7 = 14', 'MCQ', 'Easy'),
('Mathematics', 'Algebra', 'Quadratic Equations', 'Solve for x: x² - 5x + 6 = 0', 'MCQ', 'Medium'),
('Mathematics', 'Algebra', 'Systems of Equations', 'What is the value of x in: x + y = 10 and x - y = 4?', 'MCQ', 'Medium'),
('Mathematics', 'Algebra', 'Complex Equations', 'Solve: 2x² + 7x - 15 = 0', 'MCQ', 'Hard'),
('Science', 'Physics', 'Force and Motion', 'What is the SI unit of force?', 'MCQ', 'Easy'),
('Science', 'Physics', 'Newtons Laws', 'A 5kg object accelerates at 2 m/s². What is the net force?', 'MCQ', 'Medium'),
('Science', 'Physics', 'Energy Conservation', 'A 2kg ball is dropped from 10m height. What is its velocity just before impact? (g=10 m/s²)', 'MCQ', 'Hard');

-- Get the question IDs that were just inserted
-- For simplicity, we'll use last_insert_rowid() but this assumes sequential inserts

-- Insert options for Question 1 (2x + 5 = 15)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in the equation 2x + 5 = 15?'), '5', 1),
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in the equation 2x + 5 = 15?'), '10', 0),
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in the equation 2x + 5 = 15?'), '15', 0),
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in the equation 2x + 5 = 15?'), '20', 0);

-- Insert options for Question 2 (3y - 7 = 14)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'Solve for y: 3y - 7 = 14'), '5', 0),
((SELECT Id FROM Questions WHERE Text = 'Solve for y: 3y - 7 = 14'), '7', 1),
((SELECT Id FROM Questions WHERE Text = 'Solve for y: 3y - 7 = 14'), '9', 0),
((SELECT Id FROM Questions WHERE Text = 'Solve for y: 3y - 7 = 14'), '11', 0);

-- Insert options for Question 3 (x² - 5x + 6 = 0)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'Solve for x: x² - 5x + 6 = 0'), 'x = 2 or x = 3', 1),
((SELECT Id FROM Questions WHERE Text = 'Solve for x: x² - 5x + 6 = 0'), 'x = 1 or x = 6', 0),
((SELECT Id FROM Questions WHERE Text = 'Solve for x: x² - 5x + 6 = 0'), 'x = -2 or x = -3', 0),
((SELECT Id FROM Questions WHERE Text = 'Solve for x: x² - 5x + 6 = 0'), 'x = 5 or x = 1', 0);

-- Insert options for Question 4 (x + y = 10 and x - y = 4)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in: x + y = 10 and x - y = 4?'), '6', 0),
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in: x + y = 10 and x - y = 4?'), '7', 1),
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in: x + y = 10 and x - y = 4?'), '8', 0),
((SELECT Id FROM Questions WHERE Text = 'What is the value of x in: x + y = 10 and x - y = 4?'), '9', 0);

-- Insert options for Question 5 (2x² + 7x - 15 = 0)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'Solve: 2x² + 7x - 15 = 0'), 'x = 1.5 or x = -5', 1),
((SELECT Id FROM Questions WHERE Text = 'Solve: 2x² + 7x - 15 = 0'), 'x = 3 or x = -2.5', 0),
((SELECT Id FROM Questions WHERE Text = 'Solve: 2x² + 7x - 15 = 0'), 'x = 5 or x = -1.5', 0),
((SELECT Id FROM Questions WHERE Text = 'Solve: 2x² + 7x - 15 = 0'), 'x = 2 or x = -7.5', 0);

-- Insert options for Question 6 (SI unit of force)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'What is the SI unit of force?'), 'Newton', 1),
((SELECT Id FROM Questions WHERE Text = 'What is the SI unit of force?'), 'Joule', 0),
((SELECT Id FROM Questions WHERE Text = 'What is the SI unit of force?'), 'Watt', 0),
((SELECT Id FROM Questions WHERE Text = 'What is the SI unit of force?'), 'Pascal', 0);

-- Insert options for Question 7 (F = ma)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text = 'A 5kg object accelerates at 2 m/s². What is the net force?'), '7 N', 0),
((SELECT Id FROM Questions WHERE Text = 'A 5kg object accelerates at 2 m/s². What is the net force?'), '10 N', 1),
((SELECT Id FROM Questions WHERE Text = 'A 5kg object accelerates at 2 m/s². What is the net force?'), '12 N', 0),
((SELECT Id FROM Questions WHERE Text = 'A 5kg object accelerates at 2 m/s². What is the net force?'), '15 N', 0);

-- Insert options for Question 8 (velocity before impact)
INSERT INTO QuestionOptions (QuestionId, OptionText, IsCorrect) VALUES
((SELECT Id FROM Questions WHERE Text LIKE 'A 2kg ball is dropped from 10m height.%'), '10 m/s', 0),
((SELECT Id FROM Questions WHERE Text LIKE 'A 2kg ball is dropped from 10m height.%'), '14.1 m/s', 1),
((SELECT Id FROM Questions WHERE Text LIKE 'A 2kg ball is dropped from 10m height.%'), '20 m/s', 0),
((SELECT Id FROM Questions WHERE Text LIKE 'A 2kg ball is dropped from 10m height.%'), '28.3 m/s', 0);

SELECT 'Questions added successfully!' AS Result;
SELECT COUNT(*) AS TotalQuestions FROM Questions;
SELECT COUNT(*) AS TotalOptions FROM QuestionOptions;
