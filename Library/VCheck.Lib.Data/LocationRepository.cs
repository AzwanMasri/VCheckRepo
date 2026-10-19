using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VCheck.Lib.Data.Models;
using Microsoft.Extensions.Configuration;
using VCheck.Lib.Data.DBContext;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace VCheck.Lib.Data
{
    public class LocationRepository
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        /// <summary>
        /// Get Location Lists
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static List<LocationModel> GetLocationList(IConfiguration config, string clientName, string ClinicID = null)
        {
            try
            {
                using (var ctx = new LocationDBContext(config))
                {
                    if (string.IsNullOrEmpty(ClinicID))
                    {
                        return ctx.mst_location.Where(x => x.CreatedBy == clientName).ToList();
                    }
                    else
                    {
                        return ctx.mst_location.Where(x => x.ID == ClinicID).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("LocationRepository >>> GetLocationList >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Validate Is Location ID Valid
        /// </summary>
        /// <param name="config"></param>
        /// <param name="iID"></param>
        /// <returns></returns>
        public static Boolean IsLocationIdExists(IConfiguration config, string iID)
        {
            try
            {
                using (var ctx = new LocationDBContext(config))
                {
                    return ctx.mst_location.Where(x => x.ID == iID && x.Status == 1).Any();
                }
            }
            catch (Exception ex)
            {
                log.Error("LocationRepository >>> GetLocationById >>> " + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Validate Is Location ID Valid
        /// </summary>
        /// <param name="config"></param>
        /// <param name="iID"></param>
        /// <returns></returns>
        public static LocationModel GetLocationByID(IConfiguration config, string iID)
        {
            try
            {
                using (var ctx = new LocationDBContext(config))
                {
                    return ctx.mst_location.FirstOrDefault(x => x.ID == iID && x.Status == 1);
                }
            }
            catch (Exception ex)
            {
                log.Error("LocationRepository >>> GetLocationById >>> " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Validate Is Location ID Valid
        /// </summary>
        /// <param name="config"></param>
        /// <param name="iID"></param>
        /// <returns></returns>
        public static string UpdateLocation(IConfiguration config, LocationModel location)
        {
            try
            {
                using (var ctx = new LocationDBContext(config))
                {
                    var temp = ctx.mst_location.AsNoTracking().FirstOrDefault(x => (x.ID == location.ID || x.PhoneNum == location.PhoneNum || x.ID == location.PhoneNum.Replace(" ", "").Replace("-", "")) && x.ID != "");

                    if (temp != null)
                    {
                        location.UpdatedDate = DateTime.Now.ToUniversalTime();
                        location.UpdatedBy = location.CreatedBy;
                        location.CreatedDate = temp.CreatedDate;
                        location.CreatedBy = temp.CreatedBy;
                        location.ID = temp.ID;

                        ctx.mst_location.Update(location);
                    }
                    else
                    {
                        //location.ID = GenerateUniqueKey(20);
                        location.ID = string.IsNullOrEmpty(location.ID) ? location.PhoneNum.Replace(" ", "").Replace("-","") : location.ID;
                        location.CreatedDate = DateTime.Now.ToUniversalTime();

                        ctx.mst_location.Add(location);
                    }

                    ctx.SaveChanges();

                    return location.ID;
                }
            }
            catch (Exception ex)
            {
                log.Error("LocationRepository >>> GetLocationById >>> " + ex.ToString());
                return null;
            }
        }

        public static string GenerateUniqueKey(int size)
        {
            char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray(); ;
            byte[] data = new byte[4 * size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }
    }
}
