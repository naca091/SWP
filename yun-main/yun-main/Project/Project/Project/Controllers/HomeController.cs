using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.Diagnostics;

namespace Project.Controllers
{
    /*    [Authorize(Roles = "Admin")]
    */
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;

        }

        public async Task<IActionResult> Index()
        {
            // Lấy Tổng doanh thu theo tháng
            decimal totalEarnings = _context.Revenue.Sum(r => r.Earnings);
            ViewBag.TotalEarnings = totalEarnings;

            // Số lượng Note đang chờ duyệt
            int quantityToAdd = _context.Notes.Count(n => n.Status == 2);
            ViewBag.Quantity = quantityToAdd;

            // Tính tổng tất cả các Total từ Note
            int currentYear = DateTime.Now.Year;
            decimal totalFromNotes = await _context.Notes
                .Where(n => n.CreatedDate.Year == currentYear)
                .SumAsync(n => n.Total);
            ViewBag.TotalFromNotes = totalFromNotes.ToString("N0");

            // Đếm tổng số Customer
            int customerCount = await _context.Customers.CountAsync();
            ViewBag.CustomerCount = customerCount;

            return View();
        }




        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
