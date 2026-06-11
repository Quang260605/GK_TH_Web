using System;
using System.Security.Claims;
using System.Threading.Tasks;
using GK_Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GK_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user already exists
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                    return View(model);
                }

                var existingUserByName = await _userManager.FindByNameAsync(model.Username);
                if (existingUserByName != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }

                var user = new IdentityUser
                {
                    UserName = model.Username,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Người dùng mới đã được đăng ký thành công.");

                    // Assign STUDENT role by default (Question 3 requirement)
                    if (!await _roleManager.RoleExistsAsync("Student"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Student"));
                    }
                    await _userManager.AddToRoleAsync(user, "Student");

                    // Sign in immediately
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Redirect("/home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Find user by email or username
                IdentityUser? user = null;
                if (model.EmailOrUsername.Contains("@"))
                {
                    user = await _userManager.FindByEmailAsync(model.EmailOrUsername);
                }
                
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(model.EmailOrUsername);
                }

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Đăng nhập thành công.");
                        // Redirect to home (Question 5 requirement)
                        return Redirect("/home");
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Tài khoản bị khóa.");
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
                        return View(model);
                    }
                }

                ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Người dùng đã đăng xuất.");
            return Redirect("/home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // GOOGLE LOGIN INTEGRATION (Question 9)
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp dịch vụ ngoài: {remoteError}");
                return View(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("Đăng nhập bằng Google thành công.");
                return Redirect("/home");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email != null)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        // Create a new user with student role
                        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                        var createResult = await _userManager.CreateAsync(user);
                        if (createResult.Succeeded)
                        {
                            if (!await _roleManager.RoleExistsAsync("Student"))
                            {
                                await _roleManager.CreateAsync(new IdentityRole("Student"));
                            }
                            await _userManager.AddToRoleAsync(user, "Student");

                            createResult = await _userManager.AddLoginAsync(user, info);
                            if (createResult.Succeeded)
                            {
                                await _signInManager.SignInAsync(user, isPersistent: false);
                                return Redirect("/home");
                            }
                        }
                    }
                    else
                    {
                        // User exists, but doesn't have login link, so link it and sign in
                        var linkResult = await _userManager.AddLoginAsync(user, info);
                        if (linkResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return Redirect("/home");
                        }
                    }
                }

                ModelState.AddModelError(string.Empty, "Không thể đăng nhập bằng tài khoản Google này.");
                return View(nameof(Login));
            }
        }

        // ==========================================
        // MOCK GOOGLE LOGIN FOR TESTING (DEVELOPMENT MODE)
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MockGoogleLogin(string email = "googletest@registration.com")
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Register a new mock user with Student role
                var username = email.Split('@')[0] + "_google";
                user = new IdentityUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, "GoogleMock@123");
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Student"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Student"));
                    }
                    await _userManager.AddToRoleAsync(user, "Student");
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tạo tài khoản Mock Google.";
                    return RedirectToAction(nameof(Login));
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation($"Mock Google login thành công với email {email}.");
            return Redirect("/home");
        }
    }
}
