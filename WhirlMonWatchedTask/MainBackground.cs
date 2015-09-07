using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace WhirlMonWatchedTask
{
    public sealed class MainBackground : IBackgroundTask
    {
        //BackgroundTaskDeferral _deferral = null;

        static public void ClearToast()
        {
            ToastNotificationManager.History.Remove("1", "general");
        }

        static public void ShowToast(string toastText)
        {
            ToastNotifier tn = ToastNotificationManager.CreateToastNotifier();

            ClearToast();

            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(toastText));
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Tag = "1";
            toast.Group = "general";
            tn.Show(toast);
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            ShowToast("Background Task!");
            /*
            _deferral = taskInstance.GetDeferral();

            await WhirlPoolAPIClient.GetDataAsync(WhirlPoolAPIClient.EWhirlPoolData.wpAll, false);

            _deferral.Complete();
            */
        }

        static private bool taskRegistered = false;
        static private string whirlMonTaskName = "whirlMonBackgroundWatchedTask";

        static BackgroundTaskRegistration backTask = null;

        static public async void Register()
        {
            try
            {
                foreach (var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == whirlMonTaskName)
                    {
                        taskRegistered = true;
                        break;
                    }
                }

                if (taskRegistered)
                    return;

                var builder = new BackgroundTaskBuilder();

                builder.Name = whirlMonTaskName;
                builder.TaskEntryPoint = "WhirlMonWatchedTask.MainBackground";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));

                TimeTrigger minTrigger = new TimeTrigger(15, false);
                builder.SetTrigger(minTrigger);

                await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();


                backTask = builder.Register();
            }
            catch(Exception x)
            {
                ShowToast("Register Background Task:" + x.Message);
            }

            return;
        }

    }


}
