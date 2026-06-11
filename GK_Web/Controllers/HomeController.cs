using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GK_Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GK_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses(string? searchTerm, int? categoryId, int? credits, int pageNumber = 1, int pageSize = 5)
        {
            var query = _context.Courses.Include(c => c.Category).AsQueryable();

            // 1. Tìm theo tên học phần (Question 8)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm));
            }

            // 2. Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            // 3. Lọc theo số tín chỉ
            if (credits.HasValue && credits.Value > 0)
            {
                query = query.Where(c => c.Credits == credits.Value);
            }

            // 4. Phân trang (mỗi trang 5 học phần theo Câu 1)
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            if (pageNumber < 1) pageNumber = 1;
            if (totalPages > 0 && pageNumber > totalPages) pageNumber = totalPages;

            var courses = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy danh sách các học phần sinh viên đang đăng nhập đã đăng ký
            var enrolledCourseIds = new HashSet<int>();
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    enrolledCourseIds = new HashSet<int>(
                        await _context.Enrollments
                            .Where(e => e.UserId == currentUser.Id)
                            .Select(e => e.CourseId)
                            .ToListAsync()
                    );
                }
            }

            var model = new CourseListViewModel
            {
                Courses = courses,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalItems,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Credits = credits,
                EnrolledCourseIds = enrolledCourseIds
            };

            return PartialView("_CourseListPartial", model);
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
