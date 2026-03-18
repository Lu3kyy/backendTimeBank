using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlogApiPrev.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogApiPrev.Context
{
    public class DataContext : DbContext
    {
        public DbSet<UserModel> Users {get; set;}
        public DbSet<HelpPostModel> HelpPosts { get; set; }
        public DbSet<ChatThreadModel> ChatThreads { get; set; }
        public DbSet<ChatMessageModel> ChatMessages { get; set; }

        public DataContext(DbContextOptions options) : base (options){}
    }
}