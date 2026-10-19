using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheck.Lib.Data.Models;

namespace VCheck.Lib.Data.DBContext
{
    public class TestResultDBContext : DbContext
    {
        private readonly IConfiguration config = Host.CreateApplicationBuilder().Configuration;

        public DbSet<TestResultModel> txn_testResults { get; set; }
        public DbSet<TestResultDetailsModel> txn_testresults_details { get; set; }
        public DbSet<TestResultGraphModel> txn_testresults_graphs { get; set; }
        public DbSet<TestListModel> mst_testlist { get; set; }
        public DbSet<ParametersModel> mst_parameters { get; set; }
        public DbSet<TestResultNotes> tbltestanalyze_results_notes { get; set; }
        public DbSet<TestResultSpecimenContainer> tbltestanalyze_results_specimencontainer { get; set; }

        public TestResultDBContext(IConfiguration config)
        {
            this.config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(config.GetConnectionString("DefaultConnection"), ServerVersion.AutoDetect(config.GetConnectionString("DefaultConnection")));
        }
    }
}
