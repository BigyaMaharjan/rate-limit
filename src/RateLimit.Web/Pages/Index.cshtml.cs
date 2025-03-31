using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace RateLimit.Web.Pages;

public class IndexModel : RateLimitPageModel
{
    public void OnGet()
    {

    }

    public async Task OnPostLoginAsync()
    {
        await HttpContext.ChallengeAsync("oidc");
    }
}
