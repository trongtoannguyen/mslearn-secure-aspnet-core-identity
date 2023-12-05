// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using RazorPagesPizza.Areas.Identity.Data;

namespace RazorPagesPizza.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<RazorPagesPizzaUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration Configuration;

        public ConfirmEmailModel(UserManager<RazorPagesPizzaUser> userManager,
                                    RoleManager<IdentityRole> roleManager,
                                    IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            Configuration = configuration;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";

            var adminEmails = Configuration.GetSection("AdminEmail").Get<string[]>();
            if (result.Succeeded)
            {
                var isAdmin = adminEmails.Any(adminEmail => string.Compare(user.Email, adminEmail, true) == 0 ? true : false);

                if (isAdmin)
                {
                    await CheckRoleAsync("Admin");
                    await _userManager.AddToRoleAsync(user, "Admin");
                }

                await _userManager.AddClaimAsync(user,
                    new Claim("IsAdmin", isAdmin.ToString()));
            }
            return Page();
        }

        // check existing role, otherwise create specified role
        private async Task CheckRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

    }
}
