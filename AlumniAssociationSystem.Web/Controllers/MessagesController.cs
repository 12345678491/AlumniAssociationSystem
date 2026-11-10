using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Controllers;

/// <summary>模块2.6 通讯通知：站内信消息中心（邮件/短信渠道为预留扩展点）</summary>
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    public MessagesController(AppDbContext db) => _db = db;

    // ===== 消息中心（收件箱，未读优先） =====
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var messages = await _db.Messages
            .Where(m => m.RecipientId == User.GetUserId())
            .OrderBy(m => m.IsRead)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();
        ViewBag.UnreadCount = messages.Count(m => !m.IsRead);
        return View(messages);
    }

    // ===== 查看消息并标记已读 =====
    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id && m.RecipientId == User.GetUserId());
        if (message == null) return NotFound();
        message.IsRead = true;
        await _db.SaveChangesAsync();
        return View(message);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == id && m.RecipientId == User.GetUserId());
        if (message != null)
        {
            _db.Messages.Remove(message);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // ================= 管理端：群发站内信（支持按角色群发） =================
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public IActionResult Send() => View(new BroadcastViewModel());

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(BroadcastViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var recipientIds = await _db.Users
            .Where(u => u.Status == UserStatus.Approved && (vm.TargetRole == null || (int)u.Role == (int)vm.TargetRole))
            .Select(u => u.Id)
            .ToListAsync();

        _db.Messages.AddRange(recipientIds.Select(id => new Message
        { RecipientId = id, Title = vm.Title, Content = vm.Content }));
        await _db.SaveChangesAsync();
        TempData["Success"] = $"站内信已群发给 {recipientIds.Count} 位用户";
        return RedirectToAction(nameof(Send));
    }
}

public class BroadcastViewModel
{
    [Required(ErrorMessage = "请输入标题"), MaxLength(150), Display(Name = "标题")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入内容"), Display(Name = "内容")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "接收对象")]
    public UserRole? TargetRole { get; set; }
}
