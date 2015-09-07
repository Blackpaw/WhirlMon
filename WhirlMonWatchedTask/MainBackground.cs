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
            try
            {
                WhirlPoolAPIClient.LoadConfig();
                _deferral = taskInstance.GetDeferral();

                await WhirlPoolAPIClient.GetDataAsync(WhirlPoolAPIClient.EWhirlPoolData.wpAll);
            }
            catch (Exception x)
            {
                WhirlPoolAPIClient.ShowToast("MainBackground: " + x.Message);
            }
            _deferral.Complete();
        }
    }

    public sealed class ToastBackground : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral = null;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            try
            {
                WhirlPoolAPIClient.LoadConfig();
                var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
                var arguments = details.Argument;
                string[] args = arguments.Split(',');
                string sid = args.Length > 0 ? args[0] : "";
                string lastpage = args.Length > 1 ? args[1] : "";
                string lastread = args.Length > 2 ? args[2] : "";
                int id = 0;
                int.TryParse(sid, out id);

                if (lastpage != "")
                {
                    String url = string.Format(@"http://forums.whirlpool.net.au/forum-replies.cfm?t={0}&p={1}&#r{2}", sid, lastpage, lastread);
                    var uri = new Uri(url);
                    var success = await Windows.System.Launcher.LaunchUriAsync(uri);
                    if (success)
                        await WhirlPoolAPIClient.MarkThreadReadAsync(id, false);
                }
                else
                    await WhirlPoolAPIClient.MarkThreadReadAsync(id, false);
            }
            catch (Exception x)
            {
                WhirlPoolAPIClient.ShowToast("ToastBackground: " + x.Message);
            }

            _deferral.Complete();
        }
    }


}
