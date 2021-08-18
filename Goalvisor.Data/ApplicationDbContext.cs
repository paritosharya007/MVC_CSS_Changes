using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Goalvisor.Models;

namespace Goalvisor.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<ReferralLink> ReferralLinks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<EmailTemplate>();
            builder.Entity<ReferralLink>();
            builder.Entity<Subscription>().HasOne(s => s.Package);

            builder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}