using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlumniAssociationSystem.Web.Controllers;

/// <summary>模块2.7 数据统计分析 + 模块2.8 系统管理（用户审核 / 审计日志）</summary>
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // ===== 数据看板：校友 / 活动 / 捐赠 / 招聘统计 =====
    public async Task<IActionResult> Dashboard()
    {
        var now = DateTime.Now;
        var vm = new DashboardViewModel
        {
            TotalUsers = await _db.Users.CountAsync(),
            PendingUsers = await _db.Users.CountAsync(u => u.Status == UserStatus.Pending),
            ApprovedAlumni = await _db.Users.CountAsync(u => u.Role == UserRole.Alumni && u.Status == UserStatus.Approved),
            TotalEvents = await _db.Events.CountAsync(),
            ActiveEvents = await _db.Events.CountAsync(e => e.Status == EventStatus.Published),
            TotalRegistrations = await _db.EventRegistrations.CountAsync(r => r.Status != RegistrationStatus.Cancelled),
            TotalDonationAmount = await _db.DonationRecords.Where(r => r.IsPaid).SumAsync(r => r.Amount),
            DonationCount = await _db.DonationRecords.CountAsync(r => r.IsPaid),
            ActiveProjects = await _db.DonationProjects.CountAsync(p => p.Status == ProjectStatus.Active),
            PendingNews = await _db.NewsArticles.CountAsync(n => n.Status == PublishStatus.Pending),
            PendingJobs = await _db.JobPostings.CountAsync(j => j.Status == JobStatus.Pending),
            TotalApplications = await _db.JobApplications.CountAsync(),
            UnreadMessages = await _db.Messages.CountAsync(m => !m.IsRead),
            // 近30天新增校友趋势（按天）
            NewUsersByDay = await _db.Users
                .Where(u => u.CreatedAt >= now.AddDays(-30))
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new DailyCount { Date = g.Key, Count = g.Count() })
                .ToListAsync()
        };
        return View(vm);
    }

    // ===== 用户审核（自助注册 + 审核机制） =====
    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .OrderBy(u => u.Status)
            .ThenByDescending(u => u.CreatedAt)
            .ToListAsync();
        var profiles = await _db.AlumniProfiles.ToDictionaryAsync(p => p.UserId);
        ViewBag.Profiles = profiles;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AuditUser(int id, UserStatus status)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            user.Status = status;
            if (status == UserStatus.Approved)
            {
                _db.Messages.Add(new Message
                {
                    RecipientId = user.Id,
                    Title = "账号审核通过",
                    Content = $"恭喜！您的账号已审核通过，欢迎加入校友大家庭。"
                });
            }
            _db.OperationLogs.Add(new OperationLog
            { OperatorId = User.GetUserId(), OperatorName = User.GetUserName(), Action = "用户审核", Detail = $"用户 {user.Name}({user.Phone}) 审核结果：{status}" });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"用户「{user.Name}」已{(status == UserStatus.Approved ? "审核通过" : "驳回")}";
        }
        return RedirectToAction(nameof(Users));
    }

    // ===== 操作/审计日志 =====
    public async Task<IActionResult> Logs()
    {
        var logs = await _db.OperationLogs.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();
        return View(logs);
    }
}

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int PendingUsers { get; set; }
    public int ApprovedAlumni { get; set; }
    public int TotalEvents { get; set; }
    public int ActiveEvents { get; set; }
    public int TotalRegistrations { get; set; }
    public decimal TotalDonationAmount { get; set; }
    public int DonationCount { get; set; }
    public int ActiveProjects { get; set; }
    public int PendingNews { get; set; }
    public int PendingJobs { get; set; }
    public int TotalApplications { get; set; }
    public int UnreadMessages { get; set; }
    public List<DailyCount> NewUsersByDay { get; set; } = new();
}

public class DailyCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
