using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Presistance.Data
{
    public class AppDbContext : IdentityDbContext<Admin>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AbsenceExcusal> AbsenceExcusals { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<StudentSchedule> StudentSchedules { get; set; }

        //
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure StudentCourse composite key
            builder.Entity<StudentCourse>()
                .HasKey(sc => new { sc.StudentId, sc.CourseCode });

            // Configure relationships
            builder.Entity<Student>()
                .HasOne(s => s.Instructor)
                .WithMany(i => i.Students)
                .HasForeignKey(s => s.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany(i => i.Courses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Alert>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Alerts)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Lecture>()
            .HasOne(l => l.Course)
            .WithMany(c => c.Lectures)
            .HasForeignKey(l => l.CourseCode);

            builder.Entity<Lecture>()
           .HasOne(l => l.Schedule)
           .WithMany()
           .HasForeignKey(l => l.ScheduleId)
           .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Schedule>()
            .HasOne(s => s.Instructor)
            .WithMany(i => i.Schedules)
            .HasForeignKey(s => s.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StudentSchedule>()
                .HasOne(ss => ss.Student)
                .WithMany(s => s.StudentSchedules)
                .HasForeignKey(ss => ss.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentSchedule>()
                .HasOne(ss => ss.Schedule)
                .WithMany()
                .HasForeignKey(ss => ss.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attendance>()
            .HasOne(a => a.Lecture)
            .WithMany()
            .HasForeignKey(a => a.LectureId);

            // Quiz entity configuration
            builder.Entity<Quiz>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Title).IsRequired().HasMaxLength(100);
                entity.Property(q => q.DurationMinutes).IsRequired();
                entity.Property(q => q.TotalMarks).IsRequired(); // Added TotalMarks
                entity.Property(q => q.Date).IsRequired();
                entity.Property(q => q.StartTime).IsRequired();

                entity.HasOne(q => q.Course)
                      .WithMany(c => c.Quizzes) // Added navigation property
                      .HasForeignKey(q => q.CourseCode)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // QuizQuestion entity configuration
            builder.Entity<QuizQuestion>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.QuestionText).IsRequired();

                // Configure JSON serialization for Options
                entity.Property(q => q.Options)
                      .HasConversion(
                          v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                          v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
                      .HasColumnType("nvarchar(max)"); // Specify column type

                entity.Property(q => q.CorrectAnswerIndex).IsRequired();

                entity.HasOne(q => q.Quiz)
                      .WithMany(q => q.Questions)
                      .HasForeignKey(q => q.QuizId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // QuizResult entity configuration
            builder.Entity<QuizResult>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.CompletionTime).IsRequired();
                entity.Property(q => q.Score).IsRequired();
                entity.Property(q => q.Passed).IsRequired();

                entity.HasOne(q => q.Quiz)
                      .WithMany(q => q.Results)
                      .HasForeignKey(q => q.QuizId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(q => q.Student)
                      .WithMany(s => s.QuizResults) // Added navigation property
                      .HasForeignKey(q => q.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // StudentAnswer entity configuration
            builder.Entity<StudentAnswer>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.SelectedAnswerIndex).IsRequired();
                //entity.Property(s => s.IsCorrect).IsRequired();

                entity.HasOne(s => s.QuizResult)
                      .WithMany(r => r.Answers)
                      .HasForeignKey(s => s.QuizResultId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Question)
                      .WithMany()
                      .HasForeignKey(s => s.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Add these to your Student and Course entities if they don't exist
            builder.Entity<Student>(entity =>
            {
                entity.HasMany(s => s.QuizResults)
                      .WithOne(qr => qr.Student)
                      .HasForeignKey(qr => qr.StudentId);
            });

            builder.Entity<Course>(entity =>
            {
                entity.HasMany(c => c.Quizzes)
                      .WithOne(q => q.Course)
                      .HasForeignKey(q => q.CourseCode);
            });
        }
    }
}
