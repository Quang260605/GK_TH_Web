using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GK_Web.Models
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Auto-apply migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Seed Roles
            string[] roles = { "Admin", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            // Clean up old admin username if it exists in DB from previous seeds
            var oldAdmin = await userManager.FindByNameAsync("admin@registration.com");
            if (oldAdmin != null)
            {
                await userManager.DeleteAsync(oldAdmin);
            }

            // Admin User
            var adminUsername = "Admin";
            var adminUser = await userManager.FindByNameAsync(adminUsername);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminUsername,
                    Email = "admin@registration.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Student User
            var studentEmail = "student@registration.com";
            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new IdentityUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(studentUser, "Student@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");
                }
            }

            // 3. Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new Category[]
                {
                    new Category { Name = "Công nghệ thông tin" },
                    new Category { Name = "Kinh tế & Quản trị" },
                    new Category { Name = "Ngoại ngữ" },
                    new Category { Name = "Khoa học cơ bản" }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed Courses
            if (!context.Courses.Any())
            {
                var itCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Công nghệ thông tin");
                var bizCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Kinh tế & Quản trị");
                var langCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Ngoại ngữ");
                var basicCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Khoa học cơ bản");

                var courses = new Course[]
                {
                    new Course
                    {
                        Name = "Lập trình ASP.NET Core MVC",
                        Credits = 3,
                        Lecturer = "Nguyễn Văn A",
                        Image = "/images/aspnetcore.jpg",
                        CategoryId = itCategory!.Id
                    },
                    new Course
                    {
                        Name = "Cơ sở dữ liệu SQL Server",
                        Credits = 4,
                        Lecturer = "Trần Thị B",
                        Image = "/images/sqlserver.jpg",
                        CategoryId = itCategory!.Id
                    },
                    new Course
                    {
                        Name = "Quản trị kinh doanh quốc tế",
                        Credits = 3,
                        Lecturer = "Lê Hoàng C",
                        Image = "/images/business.jpg",
                        CategoryId = bizCategory!.Id
                    },
                    new Course
                    {
                        Name = "Tiếng Anh giao tiếp nâng cao",
                        Credits = 2,
                        Lecturer = "Smith John",
                        Image = "/images/english.jpg",
                        CategoryId = langCategory!.Id
                    },
                    new Course
                    {
                        Name = "Toán cao cấp A1",
                        Credits = 3,
                        Lecturer = "Phạm Minh D",
                        Image = "/images/math.jpg",
                        CategoryId = basicCategory!.Id
                    },
                    new Course
                    {
                        Name = "Lập trình hướng đối tượng C#",
                        Credits = 3,
                        Lecturer = "Vũ Văn E",
                        Image = "/images/csharp.jpg",
                        CategoryId = itCategory!.Id
                    },
                    new Course
                    {
                        Name = "Cấu trúc dữ liệu và Giải thuật",
                        Credits = 4,
                        Lecturer = "Đỗ Hoàng Nam",
                        Image = "/images/dsa.jpg",
                        CategoryId = itCategory!.Id
                    },
                    new Course
                    {
                        Name = "Phân tích và Thiết kế hệ thống",
                        Credits = 3,
                        Lecturer = "Nguyễn Hữu Chiến",
                        Image = "/images/system_design.jpg",
                        CategoryId = itCategory!.Id
                    }
                };

                context.Courses.AddRange(courses);
                await context.SaveChangesAsync();
            }
        }
    }
}
