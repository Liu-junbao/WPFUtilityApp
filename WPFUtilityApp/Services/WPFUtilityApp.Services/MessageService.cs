using WPFUtilityApp.Services.Interfaces;

namespace WPFUtilityApp.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage()
        {
            return "Hello from the Message Service";
        }
    }
}
