using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmailConsumerMQ.Model;
using Microsoft.EntityFrameworkCore;

namespace EmailConsumerMQ.Data
{
    

    public class AppDbContext : DbContext
    {
        public DbSet<EmailLog> EmailLogs { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

}
