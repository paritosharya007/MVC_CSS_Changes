namespace Goalvisor.ViewModels.Core
{
    public static class RunTimeElements
    {
        public static string JwtSecret { get; set; }
        public const string AdministratorRole = "Administrator";
        public const string SubscriberRole = "Subscriber";
        public const string UserRole = "User";
        public const string SubscriberOrAdminPolicy = "SubscriberOrAdminPolicy";
    }
}