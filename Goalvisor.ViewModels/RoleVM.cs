namespace Goalvisor.ViewModels
{
    public class RoleVM
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public bool Editable { get; set; } = true;
        public bool UserInRole { get; set; }
    }
}