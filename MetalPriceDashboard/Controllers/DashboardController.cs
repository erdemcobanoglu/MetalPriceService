using Microsoft.AspNetCore.Mvc;

namespace MetalPriceDashboard.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
