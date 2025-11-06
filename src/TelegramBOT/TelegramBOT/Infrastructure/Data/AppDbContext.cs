using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Models;
//using Telegram.Bot.Types;

namespace TelegramBOT.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Match> Matches { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Prediction> Predictions { get; set; }
        public DbSet<MatchVideo> MatchVideos { get; set; }

        public DbSet<MatchEvent> MatchEvents { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<GoalDetail> GoalDetails { get; set; }
        public DbSet<GoalieChange> GoalieChanges { get; set; }
        public DbSet<ShootoutDetail> ShootoutDetails { get; set; }
        public DbSet<Penalty> Penalties { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");

            modelBuilder.Entity<Match>().ToTable("Matches");
            modelBuilder.Entity<Team>().ToTable("Teams");

            modelBuilder.Entity<Prediction>().ToTable("Predictions");
            modelBuilder.Entity<MatchVideo>().ToTable("MatchVideos");

            modelBuilder.Entity<MatchEvent>().ToTable("MatchEvents");
            modelBuilder.Entity<EventType>().ToTable("EventTypes");
            modelBuilder.Entity<GoalDetail>().ToTable("GoalDetails");
            modelBuilder.Entity<GoalieChange>().ToTable("GoalieChanges");
            modelBuilder.Entity<ShootoutDetail>().ToTable("ShootoutDetails");
            modelBuilder.Entity<Penalty>().ToTable("Penalties");

            modelBuilder.Entity<MatchEvent>()
                .HasOne(e => e.GoalDetail)
                .WithOne(d => d.MatchEvent)
                .HasForeignKey<GoalDetail>(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchEvent>()
                .HasOne(e => e.GoalieChange)
                .WithOne(d => d.MatchEvent)
                .HasForeignKey<GoalieChange>(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchEvent>()
                .HasOne(e => e.ShootoutDetail)
                .WithOne(d => d.MatchEvent)
                .HasForeignKey<ShootoutDetail>(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchEvent>()
                .HasOne(e => e.Penalty)
                .WithOne(d => d.MatchEvent)
                .HasForeignKey<Penalty>(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
