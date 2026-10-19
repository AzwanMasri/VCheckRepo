using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VCheckListenerWorker_Others.Lib.Models;
using System.Reflection;

namespace VCheckListenerWorker_Others.Lib.DBContext
{
    public class TestResultDBContext : DbContext
    {
        private readonly IConfiguration iconfig;

        public DbSet<tbltestanalyze_results> tbltestanalyze_results { get; set; }
        public DbSet<tbltestanalyze_results_messageheader> tbltestanalyze_results_messageheader { get; set; }
        public DbSet<tbltestanalyze_results_notes> tbltestanalyze_results_notes { get; set; }
        public DbSet<tbltestanalyze_results_observationrequest> tbltestanalyze_results_observationrequest { get; set; }
        public DbSet<tbltestanalyze_results_observationresult> tbltestanalyze_results_observationresult { get; set; }
        public DbSet<tbltestanalyze_results_patientidentification> tbltestanalyze_results_patientidentification { get; set; }
        public DbSet<tbltestanalyze_results_patientvisit> tbltestanalyze_results_patientvisit { get; set; }
        public DbSet<tbltestanalyze_results_specimen> tbltestanalyze_results_specimen { get; set; }
        public DbSet<tbltestanalyze_results_specimencontainer> tbltestanalyze_results_specimencontainer { get; set; }
        public DbSet<txn_testresults> txn_Testresults { get; set; }
        public DbSet<txn_testresults_details> txn_testresults_details { get; set; }
        public DbSet<txn_testresults_graphs> txn_testresults_graphs { get; set; }
        public DbSet<mst_template> mst_template { get; set; }
        public DbSet<mst_template_details> mst_template_details { get; set; }
        public DbSet<txn_notification> txn_notification { get; set; }
        public DbSet<mst_configuration> mst_configuration { get; set; }
        public DbSet<ScheduledTestModel> Txn_ScheduledTests { get; set; }
        public DbSet<DeviceModel> mst_deviceslist { get; set; }
        public DbSet<DeviceTypeModel> mst_devicetype { get; set; }
        public DbSet<TestResultReferenceRangeModel> mst_testresults_referencerange { get; set; }

        public TestResultDBContext(IConfiguration config)
        {
            iconfig = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(iconfig.GetConnectionString("DefaultConnection"), ServerVersion.AutoDetect(iconfig.GetConnectionString("DefaultConnection")));
        }
    }
}
