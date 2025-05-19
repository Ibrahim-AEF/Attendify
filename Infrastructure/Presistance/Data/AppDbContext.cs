using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
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
        }
    }
}
