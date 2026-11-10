using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Controllers;

public class DonationsController : Controller
{
    private readonly AppDbContext _db;
    public DonationsController(AppDbContext db) => _db = db;

    // ===== 捐赠专区（项目列表 + 进度） =====
    public async Task<IActionResult> Index()
    {
        var projects = await _db.DonationProjects
            .Include(p => p.Records)
            .OrderBy(p => p.Status)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(projects);
    }

    public async Task<IActionResult> Details(int id)
    {
        var project = await _db.DonationProjects
            .Include(p => p.Records)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project == null) return NotFound();
        ViewBag.Donors = project.Records.Where(r => r.IsPaid).OrderByDescending(r => r.CreatedAt).Take(50).ToList();
        return View(project);
    }

    // ===== 在线捐赠（第三方支付为模拟对接） =====
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Donate(int id)
    {
        var project = await _db.DonationProjects.FirstOrDefaultAsync(p => p.Id == id && p.Status == ProjectStatus.Active);
        if (project == null) return NotFound();
        ViewBag.Project = project;
        return View(new DonateViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Donate(int id, DonateViewModel vm)
    {
        var project = await _db.DonationProjects.FirstOrDefaultAsync(p => p.Id == id && p.Status == ProjectStatus.Active);
        if (project == null) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewBag.Project = project;
            return View(vm);
        }

        var record = new DonationRecord
        {
            ProjectId = id, UserId = User.GetUserId(), DonorName = vm.DonorName,
            Amount = vm.Amount, IsAnonymous = vm.IsAnonymous, Message = vm.Message,
            IsPaid = true // 演示：支付网关回调直接置为已支付；实际部署在此处理第三方回调
        };
        _db.DonationRecords.Add(record);

        _db.Messages.Add(new Message
        {
            RecipientId = User.GetUserId(),
            Title = "捐赠成功",
            Content = $"感谢您对「{project.Title}」的捐赠（¥{vm.Amount:N2}），捐赠证书已生成，感谢您对母校的支持！"
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"捐赠成功！感谢您对「{project.Title}」的支持。";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ===== 我的捐赠记录 =====
    [Authorize]
    public async Task<IActionResult> Mine()
    {
        var records = await _db.DonationRecords
            .Include(r => r.Project)
            .Where(r => r.UserId == User.GetUserId())
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(records);
    }

    // ================= 管理端：捐赠项目管理 =================
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Manage()
    {
        var projects = await _db.DonationProjects
            .Include(p => p.Records)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(projects);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public IActionResult Create() => View(new DonationProject());

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DonationProject project)
    {
        if (!ModelState.IsValid) return View(project);
        _db.DonationProjects.Add(project);
        await _db.SaveChangesAsync();
        TempData["Success"] = "捐赠项目已创建";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var project = await _db.DonationProjects.FindAsync(id);
        if (project != null)
        {
            project.Status = project.Status == ProjectStatus.Active ? ProjectStatus.Closed : ProjectStatus.Active;
            await _db.SaveChangesAsync();
            TempData["Success"] = project.Status == ProjectStatus.Active ? "项目已上架" : "项目已下架";
        }
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _db.DonationProjects.FindAsync(id);
        if (project != null)
        {
            _db.DonationProjects.Remove(project);
            await _db.SaveChangesAsync();
            TempData["Success"] = "项目已删除";
        }
        return RedirectToAction(nameof(Manage));
    }
}

public class DonateViewModel
{
    [Required(ErrorMessage = "请输入捐赠人姓名"), MaxLength(50), Display(Name = "捐赠人姓名")]
    public string DonorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入捐赠金额"), Range(0.01, 10000000, ErrorMessage = "金额需大于0"), Display(Name = "捐赠金额")]
    public decimal Amount { get; set; }

    [Display(Name = "匿名捐赠")]
    public bool IsAnonymous { get; set; }

    [MaxLength(200), Display(Name = "捐赠留言")]
    public string? Message { get; set; }
}
