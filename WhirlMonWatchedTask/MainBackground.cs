using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using WhirlMonData;

namespace WhirlMonWatchedTask
{
    public sealed class BackgroundTasks
    {
        static private string whirlMonWatchedTaskName = "whirlMonBackgroundWatchedTask";
        static private string whirlMonToastTaskName = "whirlMonBackgroundToastTask";

        static BackgroundTaskRegistration backTask = null;

        private static void UnregisterBackgroundTasks()
        {
            var task = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(i => i.Name.Equals(whirlMonWatchedTaskName));
            if (task != null)
                task.Unregister(true);
            task = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(i => i.Name.Equals(whirlMonToastTaskName));
            if (task != null)
                task.Unregister(true);
        }
        static public async void Register()
        {
            try
            {
                UnregisterBackgroundTasks();

                bool taskRegistered = false;
                // Watched Task
                // Sanity check
                foreach (var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == whirlMonWatchedTaskName)
                    {
                        taskRegistered = true;
                        break;
                    }
                }
                if (!taskRegistered)
                {

                    var builder = new BackgroundTaskBuilder();

                    builder.Name = whirlMonWatchedTaskName;
                    builder.TaskEntryPoint = "WhirlMonWatchedTask.MainBackground";

                    builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));

                    TimeTrigger minTrigger = new TimeTrigger(15, false);
                    builder.SetTrigger(minTrigger);

                    await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();

                    backTask = builder.Register();
                }

                // Toast Task
                // Sanity check
                taskRegistered = false;
                foreach (var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == whirlMonToastTaskName)
                    {
                        taskRegistered = true;
                        break;
                    }
                }
                if (!taskRegistered)
                {

                    var builder = new BackgroundTaskBuilder();

                    builder.Name = whirlMonToastTaskName;
                    builder.TaskEntryPoint = "WhirlMonWatchedTask.ToastBackground";

                    builder.SetTrigger(new ToastNotificationActionTrigger());

                    await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();

                    backTask = builder.Register();
                }

            }
            catch (Exception x)
            {
                WhirlPoolAPIClient.ShowToast("Register Background Task:" + x.Message);
            }

            return;
        }


    }


    public sealed class MainBackground : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral = null;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            WhirlPoolAPIClient.LoadConfig();
            _deferral = taskInstance.GetDeferral();

            await WhirlPoolAPIClient.GetDataAsync(WhirlPoolAPIClient.EWhirlPoolData.wpAll);

            _deferral.Complete();
        }
    }

    public sealed class ToastBackground : IBackgroundTask
    {
        //BackgroundTaskDeferral _deferral = null;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            WhirlPoolAPIClient.ShowToast("Background Toast");
            WhirlPoolAPIClient.LoadConfig();
            /*_deferral = taskInstance.GetDeferral();

            await WhirlPoolAPIClient.GetDataAsync(WhirlPoolAPIClient.EWhirlPoolData.wpAll);

            _deferral.Complete();*/
        }
    }


}
