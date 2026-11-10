using AlumniAssociationSystem.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AlumniAssociationSystem.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AlumniProfile> AlumniProfiles => Set<AlumniProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<DonationProject> DonationProjects => Set<DonationProject>();
    public DbSet<DonationRecord> DonationRecords => Set<DonationRecord>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Phone).IsUnique();
        mb.Entity<AlumniProfile>().HasIndex(p => p.UserId).IsUnique();
        mb.Entity<EventRegistration>().HasIndex(r => new { r.EventId, r.UserId }).IsUnique();
        mb.Entity<DonationRecord>().HasIndex(r => r.ProjectId);
        mb.Entity<Message>().HasIndex(m => m.RecipientId);
    }
}
