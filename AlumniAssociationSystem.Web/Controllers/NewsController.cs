using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlumniAssociationSystem.Web.Controllers;

public class NewsController : Controller
{
    private readonly AppDbContext _db;
    public NewsController(AppDbContext db) => _db = db;

    // ===== 资讯列表（通知公告 / 校友动态，置顶优先） =====
    public async Task<IActionResult> Index(NewsCategory? category)
    {
        var query = _db.NewsArticles
            .Include(n => n.Author)
            .Where(n => n.Status == PublishStatus.Published)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.PublishTime);
        var list = category == null
            ? await query.Take(50).ToListAsync()
            : await query.Where(n => n.Category == category).ToListAsync();
        ViewBag.Category = category;
        return View(list);
    }

    // ===== 资讯详情（浏览量统计） =====
    public async Task<IActionResult> Details(int id)
    {
        var article = await _db.NewsArticles
            .Include(n => n.Author)
            .FirstOrDefaultAsync(n => n.Id == id && n.Status == PublishStatus.Published);
        if (article == null) return NotFound();
        article.ViewCount++;
        await _db.SaveChangesAsync();
        return View(article);
    }

    // ===== 点赞（校友动态：点赞/评论/分享） =====
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id)
    {
        var article = await _db.NewsArticles.FindAsync(id);
        if (article != null)
        {
            article.LikeCount++;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ===== 校友投稿（提交后进入待审核） =====
    [Authorize]
    [HttpGet]
    public IActionResult Submit() => View(new NewsArticle { Category = NewsCategory.Dynamic });

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(NewsArticle article)
    {
        if (!ModelState.IsValid) return View(article);
        article.AuthorId = User.GetUserId();
        article.Status = PublishStatus.Pending;
        _db.NewsArticles.Add(article);
        await _db.SaveChangesAsync();
        TempData["Success"] = "投稿成功，内容正在等待管理员审核";
        return RedirectToAction(nameof(Index));
    }

    // ================= 管理端：资讯管理 =================
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Manage()
    {
        var list = await _db.NewsArticles
            .Include(n => n.Author)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return View(list);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public IActionResult Create() => View(new NewsArticle());

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewsArticle article)
    {
        if (!ModelState.IsValid) return View(article);
        article.AuthorId = User.GetUserId();
        article.Status = PublishStatus.Published;
        article.PublishTime = DateTime.Now;
        _db.NewsArticles.Add(article);
        await _db.SaveChangesAsync();
        TempData["Success"] = "资讯已发布";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Audit(int id, PublishStatus status)
    {
        var article = await _db.NewsArticles.FindAsync(id);
        if (article != null)
        {
            article.Status = status;
            if (status == PublishStatus.Published) article.PublishTime = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["Success"] = "审核操作已完成";
        }
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePinned(int id)
    {
        var article = await _db.NewsArticles.FindAsync(id);
        if (article != null)
        {
            article.IsPinned = !article.IsPinned;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _db.NewsArticles.FindAsync(id);
        if (article != null)
        {
            _db.NewsArticles.Remove(article);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Manage));
    }
}
