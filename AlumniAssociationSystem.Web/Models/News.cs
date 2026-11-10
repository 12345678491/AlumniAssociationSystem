using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Models;

/// <summary>资讯分类（模块2.5 信息资讯）</summary>
public enum NewsCategory
{
    /// <summary>通知公告</summary>
    Notice = 0,
    /// <summary>校友动态/风采</summary>
    Dynamic = 1
}

/// <summary>发布状态（重要通知需审核后发布）</summary>
public enum PublishStatus
{
    /// <summary>待审核</summary>
    Pending = 0,
    /// <summary>已发布</summary>
    Published = 1,
    /// <summary>已驳回</summary>
    Rejected = 2
}

/// <summary>资讯/公告/校友动态</summary>
public class NewsArticle
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "标题")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "正文")]
    public string Content { get; set; } = string.Empty;

    public NewsCategory Category { get; set; } = NewsCategory.Notice;

    public PublishStatus Status { get; set; } = PublishStatus.Pending;

    /// <summary>是否置顶（公告支持置顶）</summary>
    public bool IsPinned { get; set; }

    [Display(Name = "浏览量")]
    public int ViewCount { get; set; }

    [Display(Name = "点赞数")]
    public int LikeCount { get; set; }

    public int AuthorId { get; set; }
    public User? Author { get; set; }

    [Display(Name = "发布时间")]
    public DateTime? PublishTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
