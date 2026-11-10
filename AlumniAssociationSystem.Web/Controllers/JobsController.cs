using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Controllers;

public class JobsController : Controller
{
    private readonly AppDbContext _db;
    public JobsController(AppDbContext db) => _db = db;

    // ===== 招聘专区（仅展示审核通过的职位） =====
    public async Task<IActionResult> Index()
    {
        var jobs = await _db.JobPostings
            .Where(j => j.Status == JobStatus.Approved)
            .OrderByDescending(j => j.PublishTime)
            .ToListAsync();
        return View(jobs);
    }

    public async Task<IActionResult> Details(int id)
    {
        var job = await _db.JobPostings
            .Include(j => j.PostedBy)
            .FirstOrDefaultAsync(j => j.Id == id && j.Status == JobStatus.Approved);
        if (job == null) return NotFound();
        return View(job);
    }

    // ===== 发布职位（已审核通过的校友/企业用户，提交后待管理员审核） =====
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Post()
    {
        if (await _db.Users.FindAsync(User.GetUserId()) is not { Status: UserStatus.Approved })
        {
            TempData["Error"] = "账号需审核通过后才能发布职位";
            return RedirectToAction(nameof(Index));
        }
        return View(new JobPosting());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(JobPosting job)
    {
        if (!ModelState.IsValid) return View(job);
        job.PostedById = User.GetUserId();
        job.Status = JobStatus.Pending;
        _db.JobPostings.Add(job);
        await _db.SaveChangesAsync();
        TempData["Success"] = "职位信息已提交，正在等待管理员审核";
        return RedirectToAction(nameof(Mine));
    }

    // ===== 在线投递 =====
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Apply(int id)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.Id == id && j.Status == JobStatus.Approved);
        if (job == null) return NotFound();
        ViewBag.Job = job;
        return View(new JobApplyViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int id, JobApplyViewModel vm)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.Id == id && j.Status == JobStatus.Approved);
        if (job == null) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewBag.Job = job;
            return View(vm);
        }
        if (await _db.JobApplications.AnyAsync(a => a.JobPostingId == id && a.UserId == User.GetUserId()))
        {
            TempData["Error"] = "您已投递过该职位，请勿重复投递";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.JobApplications.Add(new JobApplication
        { JobPostingId = id, UserId = User.GetUserId(), ResumeSummary = vm.ResumeSummary });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"已成功投递「{job.Title}」";
        return RedirectToAction(nameof(MyApplications));
    }

    // ===== 我发布的职位 =====
    [Authorize]
    public async Task<IActionResult> Mine()
    {
        var jobs = await _db.JobPostings
            .Include(j => j.Applications)
            .Where(j => j.PostedById == User.GetUserId())
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
        return View(jobs);
    }

    // ===== 我的投递记录 =====
    [Authorize]
    public async Task<IActionResult> MyApplications()
    {
        var apps = await _db.JobApplications
            .Include(a => a.JobPosting)
            .Where(a => a.UserId == User.GetUserId())
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return View(apps);
    }

    // ================= 管理端：招聘审核与投递管理 =================
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Manage()
    {
        var jobs = await _db.JobPostings
            .Include(j => j.PostedBy)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
        return View(jobs);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Audit(int id, JobStatus status)
    {
        var job = await _db.JobPostings.FindAsync(id);
        if (job != null)
        {
            job.Status = status;
            if (status == JobStatus.Approved) job.PublishTime = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "审核操作已完成";
        }
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.JobPostings.FindAsync(id);
        if (job != null)
        {
            _db.JobPostings.Remove(job);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage));
    }
}

public class JobApplyViewModel
{
    [Required(ErrorMessage = "请填写简历摘要或附件链接"), MaxLength(500), Display(Name = "简历摘要 / 附件链接")]
    public string ResumeSummary { get; set; } = string.Empty;
}
