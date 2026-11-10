using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Models;

/// <summary>站内信（模块2.6 通讯通知：站内信管理/已读未读/消息中心）</summary>
public class Message
{
    public int Id { get; set; }

    /// <summary>收件人</summary>
    public int RecipientId { get; set; }
    public User? Recipient { get; set; }

    [Required, MaxLength(150)]
    [Display(Name = "标题")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "内容")]
    public string Content { get; set; } = string.Empty;

    /// <summary>已读未读状态</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>操作日志（模块2.8 系统管理：操作日志与审计）</summary>
public class OperationLog
{
    public int Id { get; set; }

    public int? OperatorId { get; set; }

    [MaxLength(50)]
    [Display(Name = "操作人")]
    public string? OperatorName { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "操作类型")]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "操作详情")]
    public string? Detail { get; set; }

    [MaxLength(50)]
    [Display(Name = "来源IP")]
    public string? Ip { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
