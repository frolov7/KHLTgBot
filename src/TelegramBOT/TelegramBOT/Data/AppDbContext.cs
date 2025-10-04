using Microsoft.EntityFrameworkCore;
//using Telegram.Bot.Types;
using TelegramBOT.Models;

namespace TelegramBOT.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Match> Matches { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Prediction> Predictions { get; set; }


        public DbSet<Prediction> Predictions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");

            modelBuilder.Entity<Match>().ToTable("Matches");
            modelBuilder.Entity<Team>().ToTable("Teams");

            modelBuilder.Entity<Prediction>().ToTable("Predictions");
        }
    }
}
