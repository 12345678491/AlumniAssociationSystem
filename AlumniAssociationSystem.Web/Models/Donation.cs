using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Models;

/// <summary>捐赠项目状态</summary>
public enum ProjectStatus
{
    /// <summary>进行中</summary>
    Active = 0,
    /// <summary>已结束（下架）</summary>
    Closed = 1
}

/// <summary>捐赠项目（模块2.4 捐赠管理）</summary>
public class DonationProject
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "项目名称")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "项目介绍")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "目标金额")]
    public decimal TargetAmount { get; set; }

    [Display(Name = "截止日期")]
    public DateTime? Deadline { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<DonationRecord> Records { get; set; } = new();
}

/// <summary>捐赠记录（在线支付为模拟对接第三方网关）</summary>
public class DonationRecord
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public DonationProject? Project { get; set; }

    /// <summary>捐赠人（可匿名，未登录留空）</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "捐赠人姓名")]
    public string DonorName { get; set; } = string.Empty;

    [Display(Name = "捐赠金额")]
    public decimal Amount { get; set; }

    [Display(Name = "匿名捐赠")]
    public bool IsAnonymous { get; set; }

    [MaxLength(200)]
    [Display(Name = "捐赠留言")]
    public string? Message { get; set; }

    /// <summary>支付状态：模拟支付网关回调后置为 Paid</summary>
    public bool IsPaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
