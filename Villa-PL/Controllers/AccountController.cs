using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Store.Application.Common;
using Store.Domain.Entities;
using Store.Infrastructre.Data;
using Villa_PL.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Store.Application.Common.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Twitter;
using Villa_PL.Models;
using Microsoft.AspNetCore.Authorization;
using Octokit;
using Twilio.Types;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio;
using NToastNotify;
using Villa_PL.Helpers;
using System.Text.Encodings.Web;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Villa_PL.Controllers
{
    public class AccountController : Controller
    {

        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IToastNotification _toastNotification;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AccountController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<AppUser> signInManager,IToastNotification toastNotification)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _toastNotification = toastNotification;
         
        }
        public IActionResult Login(string returnUrl = null)
        {

            returnUrl ??= Url.Content("~/");

            LoginVM loginVM = new()
            {
                RedirectUrl = returnUrl
            };

            return View(loginVM);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Register(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!_roleManager.RoleExistsAsync(Const.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(Const.Role_Admin)).Wait();
                _roleManager.CreateAsync(new IdentityRole(Const.Role_Customer)).Wait();
            }

            RegisterVM registerVM = new()
            {
                RoleList = _roleManager.Roles.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Name
                }),
                RedirectUrl = returnUrl
            };

            return View(registerVM);
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            if (ModelState.IsValid)
            {
                var normalizedPhoneNumber = new string(registerVM.PhoneNumber.Where(char.IsDigit).ToArray());

                // Check if the phone number is already registered
                var existingPhoneNumberUser = await _userManager.FindByNameAsync(normalizedPhoneNumber);
                if (existingPhoneNumberUser != null)
                {
                    _toastNotification.AddErrorToastMessage("Phone number is already in use.");
                    return View(registerVM);
                }

                // Check if the email is already registered
                var existingEmailUser = await _userManager.FindByEmailAsync(registerVM.Email);
                if (existingEmailUser != null)
                {
                    _toastNotification.AddErrorToastMessage("Email is already in use.");
                    return View(registerVM);
                }

                // Continue with user registration if both phone number and email are unique
                AppUser user = new()
                {
                    Name = registerVM.Name,
                    Email = registerVM.Email,
                    PhoneNumber = normalizedPhoneNumber,
                    NormalizedEmail = registerVM.Email.ToUpper(),
                    CreatedAt = DateTime.Now
                };

                // Set the username to the formatted phone number
                user.UserName = normalizedPhoneNumber; // Adjust the country code as needed

                var result = await _userManager.CreateAsync(user, registerVM.Password);



                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(registerVM.Role))
                    {
                        await _userManager.AddToRoleAsync(user, registerVM.Role);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, Const.Role_Customer);
                    }

                    // Check if the user is not in the "admin" role
                    if (!await _userManager.IsInRoleAsync(user, Const.Role_Admin))
                    {
                        // Generate email confirmation token
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                        // Construct the confirmation link
                        var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);
                        var emailBody = $@"
<div style='font-family: 'Arial', sans-serif;'>
    <p style='font-size: 18px; margin-bottom: 20px; color: #333;'>
        Thank you for registering with Villa Store Sakr! To complete your registration, please click the button below to confirm your email address:
    </p>
    <div style='text-align: center;'>
        <a href='{confirmationLink}' style='
            display: inline-block;
            padding: 15px 25px;
            background-color: #4CAF50;
            color: #fff;
            text-decoration: none;
            border-radius: 5px;
            font-size: 18px;
            font-weight: bold;
            border: 2px solid #4CAF50;
            transition: background-color 0.3s, color 0.3s;'
            onmouseover='this.style.backgroundColor=""#3E8E41""; this.style.color=""#fff"";'
            onmouseout='this.style.backgroundColor=""#4CAF50""; this.style.color=""#fff"";'
        >
            Confirm Email
        </a>
    </div>
</div>
";

                        var email = new Email
                        {
                            Title = "Confirm Your Email - Villa Store Sakr",
                            Body = emailBody,
                            To = registerVM.Email
                        };

                        EmailService emailService = new EmailService();
                        emailService.SendEmail(email);

                        // Redirect to a page indicating that an email has been sent for confirmation
                        return RedirectToAction("EmailVerificationSent");
                    }
                    else
                    {
                        // User is in "admin" role, skip email confirmation
                        // Redirect to home or any other desired page
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            else
            {
                _toastNotification.AddErrorToastMessage("Register Failed");
            }

            registerVM.RoleList = _roleManager.Roles.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Name
            });

            return View(registerVM);
        }

         [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            // Determine if the input is an email or a phone number
            var isEmail = loginVM.EmailOrPhoneNumber.Contains("@");

            // If it's an email, directly find the user
            var user = isEmail
                ? await FindUserByEmailOrPhoneNumber(loginVM.EmailOrPhoneNumber)
                : await FindUserByPhoneNumber(loginVM.EmailOrPhoneNumber);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(loginVM);
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, Const.Role_Admin))
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                _toastNotification.AddErrorToastMessage("Login Failed");
                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(loginVM);
        }

        private async Task<AppUser> FindUserByPhoneNumber(string phoneNumber)
        {
            // Remove leading zeros from the phone number
            phoneNumber = phoneNumber.TrimStart('0');

            // Implement the logic to find a user by phone number (ignoring country code) in your user repository
            // Example:
            var users = await _userManager.Users.ToListAsync();  // Assuming you have a DbSet<AppUser> in your DbContext

            return users.FirstOrDefault(u => u.PhoneNumber != null && u.PhoneNumber.EndsWith(phoneNumber));
        }




        public IActionResult EmailVerificationSent()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                // Handle the error gracefully (e.g., show an error view)
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                // Handle the error gracefully (e.g., show an error view)
                return View("Error");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                // Redirect to a page indicating successful email confirmation
                return RedirectToAction("EmailVerificationSuccess");
            }

            // Handle the error gracefully (e.g., show an error view)
            return View("Error");
        }

        public IActionResult EmailVerificationSuccess()
        {
            return View();
        }


        private async Task<AppUser> FindUserByEmailOrPhoneNumber(string emailOrPhoneNumber)
        {
            // Check if it's an email
            var user = await _userManager.FindByEmailAsync(emailOrPhoneNumber);

            if (user == null)
            {
                // Check if it's a phone number
                var numericPhoneNumber = new string(emailOrPhoneNumber.Where(char.IsDigit).ToArray());
                user = await _userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == numericPhoneNumber);
            }

            return user;
        }



        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback()
        {
            var externalLoginResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (externalLoginResult?.Principal == null)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            var externalUser = externalLoginResult.Principal;
            var email = externalUser.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser == null)
            {
                var newUser = new AppUser
                {
                    Email = email,
                    UserName = email.Split('@')[0],
                    Name = email.Split('@')[0],
                };

                var createResult = await _userManager.CreateAsync(newUser);

                if (createResult.Succeeded)
                {
                    var roleExists = await _roleManager.RoleExistsAsync(Const.Role_Customer);

                    if (!roleExists)
                    {
                        var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(Const.Role_Customer));

                        if (!createRoleResult.Succeeded)
                        {
                            return RedirectToAction("ExternalLoginFailure");
                        }
                    }
                    var addToRoleResult = await _userManager.AddToRoleAsync(newUser, Const.Role_Customer);

                    if (addToRoleResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(newUser, isPersistent: false);
                    }
                    else
                    {
                        return RedirectToAction("ExternalLoginFailure");
                    }
                }
                else
                {
                    return RedirectToAction("ExternalLoginFailure");
                }
            }
            else
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
            }
            return RedirectToAction("Index", "Home");
        }


        public IActionResult ExternalLoginFailure()
        {
            return View("ExternalLoginFailure");
        }


        public async Task<IActionResult> LoginWithTwitter()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("TwitterResponse"),
                Items =
        {
            { "scheme", TwitterDefaults.AuthenticationScheme },
        },
            };

            return Challenge(properties, TwitterDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> TwitterResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result == null || !result.Succeeded)
            {
                return RedirectToAction("ExternalLoginFailure");

            }

            var claimsIdentity = result.Principal?.Identities?.FirstOrDefault();

            if (claimsIdentity != null)
            {
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;
                var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;

                if (userId == null || userName == null)
                {
                    return RedirectToAction("ExternalLoginFailure");
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    user = new AppUser
                    {
                        Id = userId,
                        UserName = userName,
                        Email = email,
                        Name = userName,
                    };

                    var createResult = await _userManager.CreateAsync(user);

                    if (!createResult.Succeeded)
                    {
                        return RedirectToAction("ExternalLoginFailure");

                    }
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordVM forgetPasswordVM)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(forgetPasswordVM.Email);

                if (user is not null)
                {
                    try
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var resetPasswordLink = Url.Action("ResetPassword", "Account", new { Email = forgetPasswordVM.Email, Token = token }, Request.Scheme);
                        var emailBody = $@"
    <div style='font-family: 'Arial', sans-serif;'>
        <p style='font-size: 18px; margin-bottom: 20px; color: #333;'>
            Dear User,
        </p>
        <p style='font-size: 18px; margin-bottom: 20px; color: #333;'>
            We received a request to reset your password. To proceed with the password reset, please click the button below:
        </p>
        <div style='text-align: center;'>
            <a href='{resetPasswordLink}' style='
                display: inline-block;
                padding: 15px 25px;
                background-color: #4CAF50;
                color: #fff;
                text-decoration: none;
                border-radius: 5px;
                font-size: 18px;
                font-weight: bold;
                border: 2px solid #4CAF50;
                transition: background-color 0.3s, color 0.3s;'
                onmouseover='this.style.backgroundColor=""#3E8E41""; this.style.color=""#fff"";'
                onmouseout='this.style.backgroundColor=""#4CAF50""; this.style.color=""#fff"";'
            >
                Reset Password
            </a>
        </div>
        <p style='font-size: 18px; margin-top: 20px; color: #333;'>
            If you did not request a password reset, please ignore this email.
        </p>
        <p style='font-size: 18px; margin-top: 20px; color: #333;'>
            Thank you for using Store Sakr!
        </p>
    </div>";

                        var email = new Email
                        {
                            Title = "🔐 Reset Your Password - Store Sakr",
                            Body = emailBody,
                            To = forgetPasswordVM.Email
                        };


                        EmailService mail = new();
                        mail.SendEmail(email);

                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        // Log the exception for troubleshooting
                        return Json(new { success = false, message = "An error occurred while processing your request. Please try again later." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Email not found. Please provide a correct email address." });
                }
            }

            // Model validation failed
            return View("ForgetPassword");
        }


        public IActionResult ForgetPassword()
        {
            return View();
        }

        public IActionResult CompleteForgetPassword()
        {
            return View();
        }
        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordVM { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM resetPasswordVM)
        {

            var user = await _userManager.FindByEmailAsync(resetPasswordVM.Email);
            if (user is not null)
            {
                var result = await _userManager.ResetPasswordAsync(user, resetPasswordVM.Token, resetPasswordVM.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordDone");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User not found. Please ensure the email and token are correct.");
            }


            return View(resetPasswordVM);
        }

        public IActionResult ResetPasswordDone()
        {
            return View();
        }


    }
}