using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisWPF
{
    class TetrisContext : DbContext
    {
        // user info table
        public DbSet<User> Users { get; set; }
        // score history table
        public DbSet<Score> Scores { get; set; }

        // configure the database connection
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Server=localhost;Database=TetrisDB;Trusted_Connection=True;Encrypt=False;";
            optionsBuilder.UseSqlServer(connectionString);
        }

        // configure the model relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // one-to-many relationship between User and Score
            // one user has many scores
            modelBuilder.Entity<User>()
                .HasMany(u => u.Scores)
                .WithOne(s => s.User)
                // config foreign key, UserId in score table is the foreign key
                .HasForeignKey(s => s.UserId);
        }
    }
}

