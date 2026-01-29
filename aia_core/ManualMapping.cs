//using aia_core.Entities;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace aia_core
//{
//    public partial class MemberClient
//    {
//        public virtual Client? Client { get; set; }
//    }

//    public partial class Client
//    {
//        public virtual MemberClient MemberClient { get; set; } = new MemberClient();
//    }


//    public partial class Context : DbContext
//    {
//        public Context()
//        {
//        }

//        public Context(DbContextOptions<Context> options)
//            : base(options)
//        {
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {

//            modelBuilder.Entity<MemberClient>(entity =>
//            {

//                entity.HasOne(d => d.Client).WithOne(p => p.MemberClient)
//                  .HasForeignKey<MemberClient>(d => d.ClientNo);

//            });
//        }

//    }
//}
