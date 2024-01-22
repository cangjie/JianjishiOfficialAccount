using System;
using Microsoft.EntityFrameworkCore;
using OA.Models;
namespace OA
{
	public class SqlServerContext: DbContext
	{
        public SqlServerContext(DbContextOptions<SqlServerContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<MiniApp.Models.MiniSession>().HasKey(c => new { c.original_id, c.session_key });
        }

        public DbSet<OARecevie> oARecevie { get; set; }
        public DbSet<OAPageAuthState> oaPageAuthState { get; set; }
        public DbSet<Token> token { get; set; }
        public DbSet<OAUser> oAUser { get; set; }
        public DbSet<User> user { get; set; }
        public DbSet<EfTest>? EfTest { get; set; }
        public DbSet<MiniUser> miniUser { get; set; }
        public DbSet<ChannelFollow> channelFollow { get; set; }
        //public DbSet<MiniApp.Models.MiniSession> miniSession { get; set; }
    }
}

