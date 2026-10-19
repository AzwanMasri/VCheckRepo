using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCheckListenerWorker_Others.Lib.Models;

namespace VCheckListenerWorker_Others.Lib.Logic
{
    public class NotificationRepository
    {
        static VCheckListenerWorker_Others.Lib.Util.Logger _logger = new VCheckListenerWorker_Others.Lib.Util.Logger();

        /// <summary>
        /// Insert Notification
        /// </summary>
        /// <param name="sResult"></param>
        public static void SendNotification(String sPatientID, String sSystemName)
        {
            try
            {
                var sConfigurationObj = TestResultRepository.GetConfigurationByKey("SystemSettings_Language");
                String sNotificationContent = "";

                //var sTemplateObj = TestResultRepository.GetNotificationTemplate("TR01");
                var sTemplateObj = TestResultRepository.GetNotificationTemplateByLanguage("TR01", (sConfigurationObj != null) ? sConfigurationObj.ConfigurationValue : "");
                if (sTemplateObj != null)
                {
                    sNotificationContent = sTemplateObj.TemplateContent;
                }

                sNotificationContent = sNotificationContent.Replace("###<patient_id>###", sPatientID);

                txn_notification sNotificationSend = new txn_notification()
                {
                    NotificationType = "Completed Test Results",
                    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                    NotificationContent = sNotificationContent,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedBy = sSystemName
                };

                TestResultRepository.insertNotification(sNotificationSend);
            }
            catch (Exception ex)
            {
                _logger.Error("Function SendNotification >>> " + ex.ToString());
            }

        }

        /// <summary>
        /// Insert Notification
        /// </summary>
        /// <param name="sResult"></param>
        public static void SendPMSErrorNotification(string sResultType, String sPatientName, String sSystemName)
        {
            try
            {
                var sConfigurationObj = TestResultRepository.GetConfigurationByKey("SystemSettings_Language");
                String sNotificationContent = "";

                var sTemplateObj = TestResultRepository.GetNotificationTemplateByLanguage("SE03", (sConfigurationObj != null) ? sConfigurationObj.ConfigurationValue : "");
                if (sTemplateObj != null)
                {
                    sNotificationContent = sTemplateObj.TemplateContent;
                }

                sNotificationContent = sNotificationContent.Replace("###<resulttype>###", sResultType).Replace("###<name>###", sPatientName);

                txn_notification sNotificationSend = new txn_notification()
                {
                    NotificationType = "Schedule Error",
                    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                    NotificationContent = sNotificationContent,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedBy = sSystemName
                };

                TestResultRepository.insertNotification(sNotificationSend);
            }
            catch (Exception ex)
            {
                _logger.Error("Function SendPMSErrorNotification >>> " + ex.ToString());
            }

        }

        /// <summary>
        /// Insert Notification
        /// </summary>
        /// <param name="sResult"></param>
        public static void SendResultMisMatchNotification(string sResultType, String sPatientID, String sSystemName)
        {
            try
            {
                var sConfigurationObj = TestResultRepository.GetConfigurationByKey("SystemSettings_Language");
                String sNotificationContent = "";

                var sTemplateObj = TestResultRepository.GetNotificationTemplateByLanguage("SE04", (sConfigurationObj != null) ? sConfigurationObj.ConfigurationValue : "");
                if (sTemplateObj != null)
                {
                    sNotificationContent = sTemplateObj.TemplateContent;
                }

                sNotificationContent = sNotificationContent.Replace("###<resulttype>###", sResultType).Replace("###<id>###", sPatientID);

                txn_notification sNotificationSend = new txn_notification()
                {
                    NotificationType = "Schedule Error",
                    NotificationTitle = (sTemplateObj != null) ? sTemplateObj.TemplateTitle : "",
                    NotificationContent = sNotificationContent,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedBy = sSystemName
                };

                TestResultRepository.insertNotification(sNotificationSend);
            }
            catch (Exception ex)
            {
                _logger.Error("Function SendResultMisMatchNotification >>> " + ex.ToString());
            }

        }
    }
}
