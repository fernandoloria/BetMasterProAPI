


using Microsoft.AspNetCore.SignalR;

namespace BetMasterApiCore.Hubs
{
    public class Messages : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }

    //public class MessagesPb : Hub
    //{
    //    public async Task SendMessage(string user, string message)
    //    {
    //        await Clients.All.SendAsync("ReceiveMessage", user, message);
    //    }
    //}
}
