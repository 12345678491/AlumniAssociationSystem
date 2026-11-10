using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlumniAssociationSystem.Web.Controllers;

public class EventsController : Controller
{
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) => _db = db;

    // ===== 活动列表（仅展示已发布活动） =====
    public async Task<IActionResult> Index()
    {
        var events = await _db.Events
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
        return View(events);
    }

    // ===== 活动详情（含报名状态与签到入口） =====
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Registrations).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null || ev.Status != EventStatus.Published && !User.IsInRole(nameof(UserRole.Admin)))
            return NotFound();

        ViewBag.MyRegistration = User.Identity!.IsAuthenticated
            ? ev.Registrations.FirstOrDefault(r => r.UserId == User.GetUserId())
            : null;
        return View(ev);
    }

    // ===== 在线报名 =====
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(int id, string? contactPhone)
    {
        var ev = await _db.Events.Include(e => e.Registrations).FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null || ev.Status != EventStatus.Published)
        {
            TempData["Error"] = "活动不存在或未开放报名";
            return RedirectToAction(nameof(Index));
        }
        if (DateTime.Now > ev.Deadline)
        {
            TempData["Error"] = "已过报名截止时间";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (await _db.EventRegistrations.AnyAsync(r => r.EventId == id && r.UserId == User.GetUserId() && r.Status == RegistrationStatus.Registered))
        {
            TempData["Error"] = "您已报名该活动，请勿重复报名";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (ev.Registrations.Count(r => r.Status == RegistrationStatus.Registered) >= ev.Capacity)
        {
            TempData["Error"] = "报名人数已满";
            return RedirectToAction(nameof(Details), new { id });
        }

        _db.EventRegistrations.Add(new EventRegistration
        { EventId = id, UserId = User.GetUserId(), ContactPhone = contactPhone ?? User.GetUserName() });
        await _db.SaveChangesAsync();

        // 模块2.6 通讯通知：站内信提醒
        _db.Messages.Add(new Message
        {
            RecipientId = User.GetUserId(),
            Title = $"报名成功：{ev.Title}",
            Content = $"您已成功报名「{ev.Title}」，活动时间 {ev.StartTime:yyyy-MM-dd HH:mm}，地点 {ev.Location}，请准时参加。"
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "报名成功！";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ===== 取消报名 =====
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var reg = await _db.EventRegistrations
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == User.GetUserId() && r.Status == RegistrationStatus.Registered);
        if (reg == null) return NotFound();
        reg.Status = RegistrationStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["Success"] = "已取消报名";
        return RedirectToAction(nameof(Mine));
    }

    // ===== 签到（演示：点击按钮模拟二维码/地理签到） =====
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        var reg = await _db.EventRegistrations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == User.GetUserId() && r.Status == RegistrationStatus.Registered);
        if (reg == null) return NotFound();
        if (DateTime.Now < reg.Event!.StartTime.AddMinutes(-30))
        {
            TempData["Error"] = "活动尚未开始，无法签到";
            return RedirectToAction(nameof(Mine));
        }
        reg.Status = RegistrationStatus.CheckedIn;
        reg.CheckInTime = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = "签到成功！";
        return RedirectToAction(nameof(Mine));
    }

    // ===== 我的报名 =====
    [Authorize]
    public async Task<IActionResult> Mine()
    {
        var list = await _db.EventRegistrations
            .Include(r => r.Event)
            .Where(r => r.UserId == User.GetUserId())
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    // ================= 管理端：活动管理 =================
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Manage()
    {
        var events = await _db.Events
            .Include(e => e.Registrations)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(events);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public IActionResult Create() => View(new Event());

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Event ev)
    {
        if (ModelState.IsValid)
        {
            ev.CreatedById = User.GetUserId();
            ev.Status = EventStatus.Published; // 管理员创建直接发布
            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            TempData["Success"] = "活动已创建并发布";
            return RedirectToAction(nameof(Manage));
        }
        return View(ev);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Audit(int id, EventStatus status)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();
        ev.Status = status;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"活动「{ev.Title}」状态已更新";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev != null)
        {
            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            TempData["Success"] = "活动已删除";
        }
        return RedirectToAction(nameof(Manage));
    }
}
