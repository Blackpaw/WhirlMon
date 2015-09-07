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
               WhirlPoolAPIClient.ShowToast("Register Background Task:" + x.Message);
            }

            return;
        }

    }


}
