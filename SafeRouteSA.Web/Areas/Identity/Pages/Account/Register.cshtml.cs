
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace SafeRouteSA.Web.Areas.Identity.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUserStore<IdentityUser> _userStore;
    private readonly IUserEmailStore<IdentityUser> _emailStore;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<IdentityUser> userManager,
        IUserStore<IdentityUser> userStore,
        SignInManager<IdentityUser> signInManager,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage = "Password must be between {2} and {1} characters.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(
            "Password",
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        ExternalLogins =
            (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync(
        string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        ReturnUrl = returnUrl;

        ExternalLogins =
            (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
            .ToList();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = CreateUser();

        await _userStore.SetUserNameAsync(
            user,
            Input.Email,
            CancellationToken.None);

        await _emailStore.SetEmailAsync(
            user,
            Input.Email,
            CancellationToken.None);

        var result = await _userManager.CreateAsync(
            user,
            Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "A new SafeRoute SA account was created.");

            var userId = await _userManager.GetUserIdAsync(user);

            var code =
                await _userManager
                    .GenerateEmailConfirmationTokenAsync(user);

            code = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new
                {
                    area = "Identity",
                    userId,
                    code,
                    returnUrl
                },
                protocol: Request.Scheme)!;

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Confirm your SafeRoute SA account",
                $"Please confirm your account by " +
                $"<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>" +
                $"clicking here</a>.");

            if (_userManager.Options.SignIn.RequireConfirmedAccount)
            {
                return RedirectToPage(
                    "RegisterConfirmation",
                    new
                    {
                        email = Input.Email,
                        returnUrl
                    });
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                string.Empty,
                error.Description);
        }

        return Page();
    }

    private IdentityUser CreateUser()
    {
        return new IdentityUser();
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
        {
            throw new NotSupportedException(
                "The user store must support email addresses.");
        }

        return (IUserEmailStore<IdentityUser>)_userStore;
    }
}

