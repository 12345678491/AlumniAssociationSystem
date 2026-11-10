using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Models;

/// <summary>活动状态（策划 → 审核 → 发布 → 结束）</summary>
public enum EventStatus
{
    /// <summary>待审核</summary>
    PendingApproval = 0,
    /// <summary>已发布（报名中）</summary>
    Published = 1,
    /// <summary>已结束</summary>
    Finished = 2,
    /// <summary>已驳回</summary>
    Rejected = 3
}

/// <summary>报名状态</summary>
public enum RegistrationStatus
{
    Registered = 0,
    Cancelled = 1,
    /// <summary>已签到</summary>
    CheckedIn = 2
}

/// <summary>校友活动（模块2.3 活动管理）</summary>
public class Event
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "活动名称")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "活动详情")]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "地点")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "开始时间")]
    public DateTime StartTime { get; set; }

    [Display(Name = "结束时间")]
    public DateTime EndTime { get; set; }

    [Display(Name = "人数上限")]
    public int Capacity { get; set; } = 100;

    [Display(Name = "报名截止时间")]
    public DateTime Deadline { get; set; }

    public EventStatus Status { get; set; } = EventStatus.PendingApproval;

    [MaxLength(500)]
    [Display(Name = "封面图地址")]
    public string? CoverUrl { get; set; }

    /// <summary>创建人</summary>
    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<EventRegistration> Registrations { get; set; } = new();
}

/// <summary>活动报名记录（报名 / 取消 / 候补 / 签到）</summary>
public class EventRegistration
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;

    [MaxLength(20)]
    [Display(Name = "联系电话")]
    public string? ContactPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "签到时间")]
    public DateTime? CheckInTime { get; set; }
}
