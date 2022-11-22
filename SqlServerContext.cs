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

        }

        public DbSet<OARecevie> oARecevie { get; set; }
        public DbSet<OAPageAuthState> oaPageAuthState { get; set; }
        public DbSet<Token> token { get; set; }
        public DbSet<OAUser> oAUser { get; set; }
        public DbSet<User> user { get; set; }
        public DbSet<OA.Models.EfTest>? EfTest { get; set; }
    }
}

