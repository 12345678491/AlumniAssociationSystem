using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Models;

/// <summary>职位审核状态（已认证校友/企业发布 → 管理员审核 → 校友端展示）</summary>
public enum JobStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>投递进度</summary>
public enum ApplicationStatus
{
    /// <summary>已投递</summary>
    Sent = 0,
    /// <summary>已查阅</summary>
    Reviewed = 1,
    /// <summary>约面试</summary>
    Interview = 2,
    /// <summary>发放Offer</summary>
    Offer = 3,
    /// <summary>不合适</summary>
    Rejected = 4
}

/// <summary>招聘信息（模块2.5 信息资讯-招聘板块）</summary>
public class JobPosting
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "公司名称")]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "职位名称")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "岗位职责")]
    public string Responsibilities { get; set; } = string.Empty;

    [Required]
    [Display(Name = "任职要求")]
    public string Requirements { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "薪资待遇")]
    public string? Salary { get; set; }

    [MaxLength(50)]
    [Display(Name = "工作地点")]
    public string? Location { get; set; }

    [MaxLength(100)]
    [Display(Name = "联系方式")]
    public string? Contact { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public int PostedById { get; set; }
    public User? PostedBy { get; set; }

    [Display(Name = "发布时间")]
    public DateTime? PublishTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<JobApplication> Applications { get; set; } = new();
}

/// <summary>职位投递记录</summary>
public class JobApplication
{
    public int Id { get; set; }

    public int JobPostingId { get; set; }
    public JobPosting? JobPosting { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(500)]
    [Display(Name = "简历摘要/附件链接")]
    public string ResumeSummary { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Sent;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
