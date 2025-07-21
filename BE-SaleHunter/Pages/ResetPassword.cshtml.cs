using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace RazorPages.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty]
        public ResetPasswordInputModel Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var apiBaseUrl = _configuration["App:BaseUrl"];
            var client = _httpClientFactory.CreateClient();
            var request = new
            {
                Email = Input.Email,
                NewPassword = Input.NewPassword,
                ConfirmNewPassword = Input.ConfirmPassword,
                Token = Token
            };

            var response = await client.PostAsJsonAsync($"{apiBaseUrl}api/Auth/reset-password", request);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Password reset successfully!";
                return RedirectToPage("/success");
            }

            var errorMessage = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Reset failed: {errorMessage}");

            return Page();
        }

        public class ResetPasswordInputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}
