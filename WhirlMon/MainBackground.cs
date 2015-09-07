using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace WhirlMon
{
    class MainBackground : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral = null;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            await WhirlMonData.WhirlPoolAPIClient.GetDataAsync();

            _deferral.Complete();
        }
    }
}
