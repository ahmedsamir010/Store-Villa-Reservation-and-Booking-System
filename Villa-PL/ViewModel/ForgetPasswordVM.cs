using System.ComponentModel.DataAnnotations;

namespace Villa_PL.ViewModel
{
    public class ForgetPasswordVM
    {

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

    }
}
