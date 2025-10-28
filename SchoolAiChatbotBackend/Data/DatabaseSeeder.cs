using Microsoft.EntityFrameworkCore;
using SchoolAiChatbotBackend.Data;
using SchoolAiChatbotBackend.Models;

namespace SchoolAiChatbotBackend.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Apply any pending migrations to create/update database schema
            await context.Database.MigrateAsync();
            
            // Check if data already exists
            if (await context.Schools.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Seed Schools
            var schools = new List<School>
            {
                new School
                {
                    Name = "Springfield Elementary School",
                    Address = "742 Evergreen Terrace, Springfield",
                    ContactInfo = "Main Office: Room 101",
                    PhoneNumber = "+1-555-0123",
                    Email = "contact@springfield-elementary.edu",
                    Website = "https://springfield-elementary.edu",
                    FeeStructure = "Kindergarten: $3000/year, Grades 1-5: $4000/year, Lunch: $500/year",
                    Timetable = "Monday-Friday 8:00 AM - 3:30 PM, Lunch: 12:00-1:00 PM",
                    Holidays = "Summer Break: June 15 - August 30, Winter Break: Dec 20 - Jan 5, Spring Break: March 15-22",
                    Events = "Science Fair: March 10, Sports Day: April 15, Graduation: June 10"
                },
                new School
                {
                    Name = "Riverside High School",
                    Address = "123 River Road, Riverside",
                    ContactInfo = "Administration Building, 2nd Floor",
                    PhoneNumber = "+1-555-0456",
                    Email = "info@riverside-high.edu",
                    Website = "https://riverside-high.edu",
                    FeeStructure = "Tuition: $8000/year, Lab Fee: $200/year, Sports Fee: $150/year",
                    Timetable = "Period 1: 8:00-8:50, Period 2: 9:00-9:50, Period 3: 10:00-10:50, Lunch: 11:00-12:00, Period 4: 12:10-1:00, Period 5: 1:10-2:00, Period 6: 2:10-3:00",
                    Holidays = "Thanksgiving: Nov 23-24, Christmas Break: Dec 20 - Jan 8, Memorial Day: May 27",
                    Events = "Homecoming: October 15, Prom: May 20, Graduation: June 15"
                },
                new School
                {
                    Name = "Oakwood Academy",
                    Address = "456 Oak Street, Oakwood",
                    ContactInfo = "Student Services Center",
                    PhoneNumber = "+1-555-0789",
                    Email = "admissions@oakwood-academy.org",
                    Website = "https://oakwood-academy.org",
                    FeeStructure = "Elementary: $5000/year, Middle School: $6000/year, High School: $7000/year",
                    Timetable = "Classes: 8:30 AM - 3:45 PM, Extended Care: 7:00 AM - 6:00 PM",
                    Holidays = "Fall Break: Oct 10-13, Winter Holiday: Dec 18 - Jan 3, Spring Break: March 25 - April 1",
                    Events = "Open House: September 20, Winter Concert: December 12, Spring Fair: April 22"
                }
            };

            context.Schools.AddRange(schools);
            await context.SaveChangesAsync();

            // Seed Users
            var users = new List<User>
            {
                new User
                {
                    Name = "John Smith",
                    Email = "john.smith@email.com",
                    Role = "Admin",
                    PasswordHash = "hashed_password_123", // In production, use proper password hashing
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id,
                    LanguagePreference = "English"
                },
                new User
                {
                    Name = "Sarah Johnson",
                    Email = "sarah.johnson@email.com",
                    Role = "Teacher",
                    PasswordHash = "hashed_password_456",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id,
                    LanguagePreference = "English"
                },
                new User
                {
                    Name = "Mike Davis",
                    Email = "mike.davis@email.com",
                    Role = "Parent",
                    PasswordHash = "hashed_password_789",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id,
                    LanguagePreference = "English"
                },
                new User
                {
                    Name = "Emma Wilson",
                    Email = "emma.wilson@email.com",
                    Role = "Student",
                    PasswordHash = "hashed_password_abc",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id,
                    LanguagePreference = "English"
                },
                new User
                {
                    Name = "Robert Brown",
                    Email = "robert.brown@email.com",
                    Role = "Teacher",
                    PasswordHash = "hashed_password_def",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[1].Id,
                    LanguagePreference = "English"
                },
                new User
                {
                    Name = "Lisa Anderson",
                    Email = "lisa.anderson@email.com",
                    Role = "Parent",
                    PasswordHash = "hashed_password_ghi",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[1].Id,
                    LanguagePreference = "Spanish"
                },
                new User
                {
                    Name = "David Miller",
                    Email = "david.miller@email.com",
                    Role = "Admin",
                    PasswordHash = "hashed_password_jkl",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[2].Id,
                    LanguagePreference = "English"
                },
                new User
                {
                    Name = "Jennifer Garcia",
                    Email = "jennifer.garcia@email.com",
                    Role = "Teacher",
                    PasswordHash = "hashed_password_mno",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[2].Id,
                    LanguagePreference = "Spanish"
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Seed UploadedFiles
            var uploadedFiles = new List<UploadedFile>
            {
                new UploadedFile
                {
                    FileName = "math_curriculum_grade5.pdf",
                    FilePath = "/uploads/math_grade5.pdf",
                    UploadDate = new DateTime(2024, 9, 1, 10, 0, 0),
                    EmbeddingDimension = 1536,
                    EmbeddingVector = "[0.1, 0.2, 0.3]"
                },
                new UploadedFile
                {
                    FileName = "science_syllabus_grade8.pdf",
                    FilePath = "/uploads/science_grade8.pdf",
                    UploadDate = new DateTime(2024, 9, 2, 14, 30, 0),
                    EmbeddingDimension = 1536,
                    EmbeddingVector = "[0.4, 0.5, 0.6]"
                },
                new UploadedFile
                {
                    FileName = "english_literature_grade10.pdf",
                    FilePath = "/uploads/english_grade10.pdf",
                    UploadDate = new DateTime(2024, 9, 3, 9, 15, 0),
                    EmbeddingDimension = 1536,
                    EmbeddingVector = "[0.7, 0.8, 0.9]"
                },
                new UploadedFile
                {
                    FileName = "history_textbook_grade7.pdf",
                    FilePath = "/uploads/history_grade7.pdf",
                    UploadDate = new DateTime(2024, 9, 4, 16, 45, 0),
                    EmbeddingDimension = 1536,
                    EmbeddingVector = "[0.2, 0.4, 0.6]"
                }
            };

            context.UploadedFiles.AddRange(uploadedFiles);
            await context.SaveChangesAsync();

            // Seed SyllabusChunks
            var syllabusChunks = new List<SyllabusChunk>
            {
                new SyllabusChunk
                {
                    Subject = "Mathematics",
                    Grade = "Grade 5",
                    Source = "Common Core Standards",
                    ChunkText = "Students will learn multiplication and division of multi-digit numbers. They will understand place value and use it to perform operations.",
                    Chapter = "Chapter 1: Number Operations",
                    UploadedFileId = uploadedFiles[0].Id,
                    PineconeVectorId = "math-grade5-chunk-001"
                },
                new SyllabusChunk
                {
                    Subject = "Mathematics",
                    Grade = "Grade 5",
                    Source = "Common Core Standards",
                    ChunkText = "Introduction to fractions and decimals. Students will add and subtract fractions with like denominators.",
                    Chapter = "Chapter 2: Fractions",
                    UploadedFileId = uploadedFiles[0].Id,
                    PineconeVectorId = "math-grade5-chunk-002"
                },
                new SyllabusChunk
                {
                    Subject = "Science",
                    Grade = "Grade 8",
                    Source = "Next Generation Science Standards",
                    ChunkText = "Earth and Space Sciences: Understanding the solar system, planets, and their characteristics.",
                    Chapter = "Chapter 3: Solar System",
                    UploadedFileId = uploadedFiles[1].Id,
                    PineconeVectorId = "science-grade8-chunk-001"
                },
                new SyllabusChunk
                {
                    Subject = "Science",
                    Grade = "Grade 8",
                    Source = "Next Generation Science Standards",
                    ChunkText = "Physical Science: Introduction to atoms, molecules, and chemical reactions.",
                    Chapter = "Chapter 4: Chemistry Basics",
                    UploadedFileId = uploadedFiles[1].Id,
                    PineconeVectorId = "science-grade8-chunk-002"
                },
                new SyllabusChunk
                {
                    Subject = "English Literature",
                    Grade = "Grade 10",
                    Source = "State Standards",
                    ChunkText = "Reading comprehension strategies for poetry and prose. Analysis of literary devices and themes.",
                    Chapter = "Chapter 5: Literary Analysis",
                    UploadedFileId = uploadedFiles[2].Id,
                    PineconeVectorId = "english-grade10-chunk-001"
                },
                new SyllabusChunk
                {
                    Subject = "History",
                    Grade = "Grade 7",
                    Source = "Social Studies Standards",
                    ChunkText = "American Revolution: Causes, key events, and consequences of the Revolutionary War.",
                    Chapter = "Chapter 6: American Revolution",
                    UploadedFileId = uploadedFiles[3].Id,
                    PineconeVectorId = "history-grade7-chunk-001"
                }
            };

            context.SyllabusChunks.AddRange(syllabusChunks);
            await context.SaveChangesAsync();

            // Seed FAQs
            var faqs = new List<Faq>
            {
                new Faq
                {
                    Question = "What time does school start?",
                    Answer = "School starts at 8:00 AM for all grades. Please ensure students arrive by 7:55 AM.",
                    Category = "General",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id
                },
                new Faq
                {
                    Question = "How can I contact my child's teacher?",
                    Answer = "You can contact teachers through the school portal, email, or by calling the main office at +1-555-0123.",
                    Category = "Communication",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id
                },
                new Faq
                {
                    Question = "What is the dress code policy?",
                    Answer = "Students should wear appropriate casual clothing. No shorts above the knee, tank tops, or clothing with inappropriate messages.",
                    Category = "Policies",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id
                },
                new Faq
                {
                    Question = "When is lunch served?",
                    Answer = "Lunch is served from 12:00 PM to 1:00 PM. The cafeteria offers both hot meals and salad bar options.",
                    Category = "Meals",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id
                },
                new Faq
                {
                    Question = "How do I enroll my child?",
                    Answer = "Visit our website at https://springfield-elementary.edu or come to the main office with required documents.",
                    Category = "Enrollment",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[0].Id
                },
                new Faq
                {
                    Question = "What are the school hours?",
                    Answer = "Classes run from 8:00 AM to 3:30 PM, Monday through Friday.",
                    Category = "General",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[1].Id
                },
                new Faq
                {
                    Question = "Is there a uniform policy?",
                    Answer = "Yes, students must wear navy blue pants/skirts with white or light blue shirts. PE uniform required for gym class.",
                    Category = "Policies",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[1].Id
                },
                new Faq
                {
                    Question = "How can I check my grades?",
                    Answer = "Students and parents can access grades through our online portal using their login credentials.",
                    Category = "Academic",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[1].Id
                },
                new Faq
                {
                    Question = "What extracurricular activities are available?",
                    Answer = "We offer sports teams, drama club, debate team, chess club, and various honor societies.",
                    Category = "Activities",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[2].Id
                },
                new Faq
                {
                    Question = "How do I report an absence?",
                    Answer = "Call the attendance office at +1-555-0789 before 9:00 AM or use the online absence reporting system.",
                    Category = "Attendance",
                    CreatedAt = DateTime.UtcNow,
                    SchoolId = schools[2].Id
                }
            };

            context.Faqs.AddRange(faqs);
            await context.SaveChangesAsync();

            // Seed Embeddings
            var embeddings = new List<Embedding>
            {
                new Embedding
                {
                    SyllabusChunkId = syllabusChunks[0].Id,
                    VectorJson = "[0.123, 0.456, 0.789, 0.234, 0.567]",
                    SchoolId = schools[0].Id
                },
                new Embedding
                {
                    SyllabusChunkId = syllabusChunks[1].Id,
                    VectorJson = "[0.345, 0.678, 0.901, 0.456, 0.789]",
                    SchoolId = schools[0].Id
                },
                new Embedding
                {
                    SyllabusChunkId = syllabusChunks[2].Id,
                    VectorJson = "[0.567, 0.890, 0.123, 0.678, 0.901]",
                    SchoolId = schools[1].Id
                },
                new Embedding
                {
                    SyllabusChunkId = syllabusChunks[3].Id,
                    VectorJson = "[0.789, 0.012, 0.345, 0.890, 0.123]",
                    SchoolId = schools[1].Id
                },
                new Embedding
                {
                    SyllabusChunkId = syllabusChunks[4].Id,
                    VectorJson = "[0.901, 0.234, 0.567, 0.012, 0.345]",
                    SchoolId = schools[2].Id
                },
                new Embedding
                {
                    SyllabusChunkId = syllabusChunks[5].Id,
                    VectorJson = "[0.123, 0.456, 0.789, 0.234, 0.567]",
                    SchoolId = schools[2].Id
                }
            };

            context.Embeddings.AddRange(embeddings);
            await context.SaveChangesAsync();

            // Seed ChatLogs
            var chatLogs = new List<ChatLog>
            {
                new ChatLog
                {
                    UserId = users[3].Id, // Emma Wilson (Student)
                    Message = "What time does school start?",
                    Response = "School starts at 8:00 AM for all grades. Please ensure students arrive by 7:55 AM to be ready for class.",
                    Timestamp = new DateTime(2024, 10, 20, 8, 30, 0),
                    SchoolId = schools[0].Id
                },
                new ChatLog
                {
                    UserId = users[2].Id, // Mike Davis (Parent)
                    Message = "How can I check my child's grades?",
                    Response = "You can check grades through our parent portal. Login with your credentials at our school website or contact the office for assistance.",
                    Timestamp = new DateTime(2024, 10, 20, 14, 15, 0),
                    SchoolId = schools[0].Id
                },
                new ChatLog
                {
                    UserId = users[3].Id, // Emma Wilson (Student)
                    Message = "What's for lunch today?",
                    Response = "Today's lunch menu includes chicken nuggets, mac and cheese, fresh fruit, and milk. Vegetarian options are also available.",
                    Timestamp = new DateTime(2024, 10, 20, 11, 45, 0),
                    SchoolId = schools[0].Id
                },
                new ChatLog
                {
                    UserId = users[5].Id, // Lisa Anderson (Parent)
                    Message = "When is the next parent-teacher conference?",
                    Response = "Parent-teacher conferences are scheduled for November 15-16. Please sign up for a time slot through the online scheduling system.",
                    Timestamp = new DateTime(2024, 10, 19, 16, 20, 0),
                    SchoolId = schools[1].Id
                },
                new ChatLog
                {
                    UserId = users[7].Id, // Jennifer Garcia (Teacher)
                    Message = "What are the graduation requirements?",
                    Response = "Students need 24 credits including 4 years English, 3 years math, 3 years science, 3 years social studies, and electives.",
                    Timestamp = new DateTime(2024, 10, 18, 10, 30, 0),
                    SchoolId = schools[2].Id
                }
            };

            context.ChatLogs.AddRange(chatLogs);
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeded successfully!");
        }
    }
}