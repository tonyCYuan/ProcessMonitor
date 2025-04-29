using Microsoft.AspNetCore.SignalR;
using ProcessMonitor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessMonitor.Hubs
{
    public class ProcessHub : Hub
    {
        public async Task SendProcessUpdates(List<ProcessInfo> processes)
        {
            await Clients.All.SendAsync("ReceiveProcessUpdates", processes);
        }

        public async Task SendProcessDetails(ProcessInfo process)
        {
            await Clients.All.SendAsync("ReceiveProcessDetails", process);
        }

        public async Task SendSuspiciousProcessAlert(ProcessInfo process, string alertMessage)
        {
            await Clients.All.SendAsync("ReceiveSuspiciousProcessAlert", process, alertMessage);
        }
    }
}