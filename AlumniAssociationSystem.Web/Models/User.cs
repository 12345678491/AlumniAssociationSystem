using System.ComponentModel.DataAnnotations;

namespace AlumniAssociationSystem.Web.Models;

/// <summary>用户角色（RBAC）</summary>
public enum UserRole
{
    /// <summary>校友（普通用户）</summary>
    Alumni = 0,
    /// <summary>管理员</summary>
    Admin = 1
}

/// <summary>账号状态：注册后需管理员审核（自助注册 + 审核机制）</summary>
public enum UserStatus
{
    /// <summary>待审核</summary>
    Pending = 0,
    /// <summary>已通过</summary>
    Approved = 1,
    /// <summary>已驳回</summary>
    Rejected = 2
}

/// <summary>用户账户（模块2.1 用户认证与权限管理）</summary>
public class User
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    [Display(Name = "手机号")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "邮箱")]
    public string? Email { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Alumni;

    public UserStatus Status { get; set; } = UserStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>校友档案（模块2.2 校友信息管理）</summary>
public class AlumniProfile
{
    public int Id { get; set; }

    /// <summary>关联账户（一对一）</summary>
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "学号")]
    public string StudentNo { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "毕业年份")]
    public int GradYear { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "院系")]
    public string Department { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Display(Name = "专业")]
    public string Major { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "所在行业")]
    public string? Industry { get; set; }

    [MaxLength(50)]
    [Display(Name = "所在城市")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Display(Name = "工作单位")]
    public string? Company { get; set; }

    [MaxLength(50)]
    [Display(Name = "职务")]
    public string? Position { get; set; }

    [MaxLength(500)]
    [Display(Name = "个人简介")]
    public string? Bio { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
