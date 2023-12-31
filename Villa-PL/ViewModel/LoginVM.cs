using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;

namespace Villa_PL.ViewModel
{
    public class LoginVM
    {

        [Required]
        [Display(Name = "Email or Phone Number")]
        public string EmailOrPhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string? RedirectUrl { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        // Add this property for phone number
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please select the country code.")]
        [Display(Name = "Country Code")]
        public string CountryCode { get; set; }

    }

}
