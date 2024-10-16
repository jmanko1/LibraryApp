using System.ComponentModel.DataAnnotations;

namespace BookApp.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
