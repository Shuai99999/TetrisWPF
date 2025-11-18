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
        public DbSet<User> Users { get; set; }       // 用户信息表
        public DbSet<Score> Scores { get; set; }     // 历史成绩表

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Server=localhost;Database=TetrisDB;Trusted_Connection=True;Encrypt=False;";
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 可以在这里配置关系和初始数据
            modelBuilder.Entity<User>()
                .HasMany(u => u.Scores)
                .WithOne(s => s.User)
                // 配置外键
                .HasForeignKey(s => s.UserId);
        }
    }
}

