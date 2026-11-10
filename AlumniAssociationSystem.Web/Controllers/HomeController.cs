using AlumniAssociationSystem.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlumniAssociationSystem.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    // ===== 首页：最新公告 / 热门活动 / 捐赠项目一览 =====
    public async Task<IActionResult> Index()
    {
        ViewBag.Notices = await _db.NewsArticles
            .Where(n => n.Status == Models.PublishStatus.Published && n.Category == Models.NewsCategory.Notice)
            .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.PublishTime)
            .Take(5).ToListAsync();
        ViewBag.Events = await _db.Events
            .Include(e => e.Registrations)
            .Where(e => e.Status == Models.EventStatus.Published)
            .OrderBy(e => e.StartTime).Take(4).ToListAsync();
        ViewBag.Projects = await _db.DonationProjects
            .Include(p => p.Records)
            .Where(p => p.Status == Models.ProjectStatus.Active)
            .OrderByDescending(p => p.CreatedAt).Take(3).ToListAsync();
        ViewBag.Jobs = await _db.JobPostings
            .Where(j => j.Status == Models.JobStatus.Approved)
            .OrderByDescending(j => j.PublishTime).Take(4).ToListAsync();
        ViewBag.AlumniCount = await _db.Users.CountAsync(u => u.Role == Models.UserRole.Alumni && u.Status == Models.UserStatus.Approved);
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
