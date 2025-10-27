-- School AI Chatbot Database Setup Script for MySQL
-- This script creates all necessary tables and inserts sample test data

-- Note: Run this script on MySQL database 'flexibleserverdb'

USE flexibleserverdb;

-- 1. Create Schools table
CREATE TABLE IF NOT EXISTS Schools (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Address VARCHAR(500),
    ContactInfo VARCHAR(500),
    PhoneNumber VARCHAR(20) DEFAULT '',
    Email VARCHAR(255) DEFAULT '',
    Website VARCHAR(255) DEFAULT '',
    FeeStructure TEXT,
    Timetable TEXT,
    Holidays TEXT,
    Events TEXT
);

-- 2. Create Users table
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    Role VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    SchoolId INT NOT NULL,
    LanguagePreference VARCHAR(50),
    FOREIGN KEY (SchoolId) REFERENCES Schools(Id) ON DELETE CASCADE
);

-- 3. Create UploadedFiles table
CREATE TABLE IF NOT EXISTS UploadedFiles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FileName VARCHAR(255) NOT NULL DEFAULT '',
    FilePath VARCHAR(500) NOT NULL DEFAULT '',
    UploadDate DATETIME NOT NULL,
    EmbeddingDimension INT NOT NULL,
    EmbeddingVector TEXT NOT NULL DEFAULT ''
);

-- 4. Create SyllabusChunks table
CREATE TABLE IF NOT EXISTS SyllabusChunks (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Subject VARCHAR(255) NOT NULL DEFAULT '',
    Grade VARCHAR(50) NOT NULL DEFAULT '',
    Source VARCHAR(255) NOT NULL DEFAULT '',
    ChunkText TEXT NOT NULL DEFAULT '',
    Chapter VARCHAR(255) NOT NULL DEFAULT '',
    UploadedFileId INT NOT NULL,
    PineconeVectorId VARCHAR(255) NOT NULL DEFAULT '',
    FOREIGN KEY (UploadedFileId) REFERENCES UploadedFiles(Id) ON DELETE CASCADE
);

-- 5. Create Faqs table
CREATE TABLE IF NOT EXISTS Faqs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Question TEXT NOT NULL,
    Answer TEXT NOT NULL,
    Category VARCHAR(100) DEFAULT 'General',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    SchoolId INT NOT NULL,
    FOREIGN KEY (SchoolId) REFERENCES Schools(Id) ON DELETE CASCADE
);

-- 6. Create Embeddings table
CREATE TABLE IF NOT EXISTS Embeddings (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SyllabusChunkId INT NOT NULL,
    VectorJson TEXT NOT NULL,
    SchoolId INT NOT NULL,
    FOREIGN KEY (SyllabusChunkId) REFERENCES SyllabusChunks(Id) ON DELETE RESTRICT,
    FOREIGN KEY (SchoolId) REFERENCES Schools(Id) ON DELETE CASCADE
);

-- 7. Create ChatLogs table
CREATE TABLE IF NOT EXISTS ChatLogs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Message TEXT NOT NULL,
    Response TEXT NOT NULL,
    Timestamp DATETIME NOT NULL,
    SchoolId INT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE RESTRICT,
    FOREIGN KEY (SchoolId) REFERENCES Schools(Id) ON DELETE CASCADE
);

-- =========================================
-- INSERT SAMPLE TEST DATA
-- =========================================

-- Insert Sample Schools
INSERT INTO Schools (Name, Address, ContactInfo, PhoneNumber, Email, Website, FeeStructure, Timetable, Holidays, Events) VALUES
('Springfield Elementary School', '742 Evergreen Terrace, Springfield', 'Main Office: Room 101', '+1-555-0123', 'contact@springfield-elementary.edu', 'https://springfield-elementary.edu', 
'Kindergarten: $3000/year, Grades 1-5: $4000/year, Lunch: $500/year', 
'Monday-Friday 8:00 AM - 3:30 PM, Lunch: 12:00-1:00 PM', 
'Summer Break: June 15 - August 30, Winter Break: Dec 20 - Jan 5, Spring Break: March 15-22', 
'Science Fair: March 10, Sports Day: April 15, Graduation: June 10'),

('Riverside High School', '123 River Road, Riverside', 'Administration Building, 2nd Floor', '+1-555-0456', 'info@riverside-high.edu', 'https://riverside-high.edu',
'Tuition: $8000/year, Lab Fee: $200/year, Sports Fee: $150/year',
'Period 1: 8:00-8:50, Period 2: 9:00-9:50, Period 3: 10:00-10:50, Lunch: 11:00-12:00, Period 4: 12:10-1:00, Period 5: 1:10-2:00, Period 6: 2:10-3:00',
'Thanksgiving: Nov 23-24, Christmas Break: Dec 20 - Jan 8, Memorial Day: May 27',
'Homecoming: October 15, Prom: May 20, Graduation: June 15'),

('Oakwood Academy', '456 Oak Street, Oakwood', 'Student Services Center', '+1-555-0789', 'admissions@oakwood-academy.org', 'https://oakwood-academy.org',
'Elementary: $5000/year, Middle School: $6000/year, High School: $7000/year',
'Classes: 8:30 AM - 3:45 PM, Extended Care: 7:00 AM - 6:00 PM',
'Fall Break: Oct 10-13, Winter Holiday: Dec 18 - Jan 3, Spring Break: March 25 - April 1',
'Open House: September 20, Winter Concert: December 12, Spring Fair: April 22');

-- Insert Sample Users
INSERT INTO Users (Name, Email, Role, PasswordHash, SchoolId, LanguagePreference) VALUES
('John Smith', 'john.smith@email.com', 'Admin', 'hashed_password_123', 1, 'English'),
('Sarah Johnson', 'sarah.johnson@email.com', 'Teacher', 'hashed_password_456', 1, 'English'),
('Mike Davis', 'mike.davis@email.com', 'Parent', 'hashed_password_789', 1, 'English'),
('Emma Wilson', 'emma.wilson@email.com', 'Student', 'hashed_password_abc', 1, 'English'),
('Robert Brown', 'robert.brown@email.com', 'Teacher', 'hashed_password_def', 2, 'English'),
('Lisa Anderson', 'lisa.anderson@email.com', 'Parent', 'hashed_password_ghi', 2, 'Spanish'),
('David Miller', 'david.miller@email.com', 'Admin', 'hashed_password_jkl', 3, 'English'),
('Jennifer Garcia', 'jennifer.garcia@email.com', 'Teacher', 'hashed_password_mno', 3, 'Spanish');

-- Insert Sample UploadedFiles
INSERT INTO UploadedFiles (FileName, FilePath, UploadDate, EmbeddingDimension, EmbeddingVector) VALUES
('math_curriculum_grade5.pdf', '/uploads/math_grade5.pdf', '2024-09-01 10:00:00', 1536, '[0.1, 0.2, 0.3]'),
('science_syllabus_grade8.pdf', '/uploads/science_grade8.pdf', '2024-09-02 14:30:00', 1536, '[0.4, 0.5, 0.6]'),
('english_literature_grade10.pdf', '/uploads/english_grade10.pdf', '2024-09-03 09:15:00', 1536, '[0.7, 0.8, 0.9]'),
('history_textbook_grade7.pdf', '/uploads/history_grade7.pdf', '2024-09-04 16:45:00', 1536, '[0.2, 0.4, 0.6]');

-- Insert Sample SyllabusChunks
INSERT INTO SyllabusChunks (Subject, Grade, Source, ChunkText, Chapter, UploadedFileId, PineconeVectorId) VALUES
('Mathematics', 'Grade 5', 'Common Core Standards', 'Students will learn multiplication and division of multi-digit numbers. They will understand place value and use it to perform operations.', 'Chapter 1: Number Operations', 1, 'math-grade5-chunk-001'),
('Mathematics', 'Grade 5', 'Common Core Standards', 'Introduction to fractions and decimals. Students will add and subtract fractions with like denominators.', 'Chapter 2: Fractions', 1, 'math-grade5-chunk-002'),
('Science', 'Grade 8', 'Next Generation Science Standards', 'Earth and Space Sciences: Understanding the solar system, planets, and their characteristics.', 'Chapter 3: Solar System', 2, 'science-grade8-chunk-001'),
('Science', 'Grade 8', 'Next Generation Science Standards', 'Physical Science: Introduction to atoms, molecules, and chemical reactions.', 'Chapter 4: Chemistry Basics', 2, 'science-grade8-chunk-002'),
('English Literature', 'Grade 10', 'State Standards', 'Reading comprehension strategies for poetry and prose. Analysis of literary devices and themes.', 'Chapter 5: Literary Analysis', 3, 'english-grade10-chunk-001'),
('History', 'Grade 7', 'Social Studies Standards', 'American Revolution: Causes, key events, and consequences of the Revolutionary War.', 'Chapter 6: American Revolution', 4, 'history-grade7-chunk-001');

-- Insert Sample FAQs
INSERT INTO Faqs (Question, Answer, Category, SchoolId) VALUES
('What time does school start?', 'School starts at 8:00 AM for all grades. Please ensure students arrive by 7:55 AM.', 'General', 1),
('How can I contact my child''s teacher?', 'You can contact teachers through the school portal, email, or by calling the main office at +1-555-0123.', 'Communication', 1),
('What is the dress code policy?', 'Students should wear appropriate casual clothing. No shorts above the knee, tank tops, or clothing with inappropriate messages.', 'Policies', 1),
('When is lunch served?', 'Lunch is served from 12:00 PM to 1:00 PM. The cafeteria offers both hot meals and salad bar options.', 'Meals', 1),
('How do I enroll my child?', 'Visit our website at https://springfield-elementary.edu or come to the main office with required documents.', 'Enrollment', 1),
('What are the school hours?', 'Classes run from 8:00 AM to 3:30 PM, Monday through Friday.', 'General', 2),
('Is there a uniform policy?', 'Yes, students must wear navy blue pants/skirts with white or light blue shirts. PE uniform required for gym class.', 'Policies', 2),
('How can I check my grades?', 'Students and parents can access grades through our online portal using their login credentials.', 'Academic', 2),
('What extracurricular activities are available?', 'We offer sports teams, drama club, debate team, chess club, and various honor societies.', 'Activities', 3),
('How do I report an absence?', 'Call the attendance office at +1-555-0789 before 9:00 AM or use the online absence reporting system.', 'Attendance', 3);

-- Insert Sample Embeddings
INSERT INTO Embeddings (SyllabusChunkId, VectorJson, SchoolId) VALUES
(1, '[0.123, 0.456, 0.789, 0.234, 0.567]', 1),
(2, '[0.345, 0.678, 0.901, 0.456, 0.789]', 1),
(3, '[0.567, 0.890, 0.123, 0.678, 0.901]', 2),
(4, '[0.789, 0.012, 0.345, 0.890, 0.123]', 2),
(5, '[0.901, 0.234, 0.567, 0.012, 0.345]', 3),
(6, '[0.123, 0.456, 0.789, 0.234, 0.567]', 3);

-- Insert Sample Chat Logs
INSERT INTO ChatLogs (UserId, Message, Response, Timestamp, SchoolId) VALUES
(4, 'What time does school start?', 'School starts at 8:00 AM for all grades. Please ensure students arrive by 7:55 AM to be ready for class.', '2024-10-20 08:30:00', 1),
(3, 'How can I check my child''s grades?', 'You can check grades through our parent portal. Login with your credentials at our school website or contact the office for assistance.', '2024-10-20 14:15:00', 1),
(4, 'What''s for lunch today?', 'Today''s lunch menu includes chicken nuggets, mac and cheese, fresh fruit, and milk. Vegetarian options are also available.', '2024-10-20 11:45:00', 1),
(6, 'When is the next parent-teacher conference?', 'Parent-teacher conferences are scheduled for November 15-16. Please sign up for a time slot through the online scheduling system.', '2024-10-19 16:20:00', 2),
(8, 'What are the graduation requirements?', 'Students need 24 credits including 4 years English, 3 years math, 3 years science, 3 years social studies, and electives.', '2024-10-18 10:30:00', 3);

-- Display success message
SELECT 'Database tables created and sample data inserted successfully!' as Status;

-- Show record counts for verification
SELECT 'Schools' as TableName, COUNT(*) as RecordCount FROM Schools
UNION ALL
SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'UploadedFiles' as TableName, COUNT(*) as RecordCount FROM UploadedFiles
UNION ALL
SELECT 'SyllabusChunks' as TableName, COUNT(*) as RecordCount FROM SyllabusChunks
UNION ALL
SELECT 'Faqs' as TableName, COUNT(*) as RecordCount FROM Faqs
UNION ALL
SELECT 'Embeddings' as TableName, COUNT(*) as RecordCount FROM Embeddings
UNION ALL
SELECT 'ChatLogs' as TableName, COUNT(*) as RecordCount FROM ChatLogs;