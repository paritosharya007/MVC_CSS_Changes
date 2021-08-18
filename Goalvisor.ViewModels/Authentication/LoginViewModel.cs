﻿using System.ComponentModel.DataAnnotations;

namespace Goalvisor.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
        public bool IsNew { get; set; }
    }
}