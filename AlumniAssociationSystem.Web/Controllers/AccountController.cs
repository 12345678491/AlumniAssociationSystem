using AlumniAssociationSystem.Web.Data;
using AlumniAssociationSystem.Web.Extensions;
using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AlumniAssociationSystem.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    // ===== 注册（自助注册 + 验证码校验，注册后待管理员审核） =====
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        // 模拟验证码校验（演示模式：验证码显示在页面上；实际部署应改为短信/邮件网关发送）
        var code = HttpContext.Session.GetString("RegisterVerifyCode") ?? "";
        if (string.IsNullOrEmpty(code))
        {
            code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("RegisterVerifyCode", code);
        }
        vm.GeneratedVerifyCode = code;

        if (!ModelState.IsValid) return View(vm);
        if (vm.VerifyCode != code)
        {
            ModelState.AddModelError(nameof(vm.VerifyCode), "验证码不正确");
            return View(vm);
        }
        if (await _db.Users.AnyAsync(u => u.Phone == vm.Phone))
        {
            ModelState.AddModelError(nameof(vm.Phone), "该手机号已注册");
            return View(vm);
        }

        var user = new User
        {
            Phone = vm.Phone, Email = vm.Email, Name = vm.Name,
            Role = UserRole.Alumni, Status = UserStatus.Pending
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, vm.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 同步创建校友档案（核心身份信息：学号/毕业年份/院系/专业）
        _db.AlumniProfiles.Add(new AlumniProfile
        {
            UserId = user.Id, StudentNo = vm.StudentNo, Name = vm.Name,
            GradYear = vm.GradYear, Department = vm.Department, Major = vm.Major
        });
        await _db.SaveChangesAsync();

        HttpContext.Session.Remove("RegisterVerifyCode");
        TempData["Success"] = "注册成功！您的账号正在等待管理员审核，审核通过后方可登录。";
        return RedirectToAction(nameof(Login));
    }

    // ===== 登录 =====
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == vm.Account || u.Email == vm.Account);
        if (user == null)
        {
            ModelState.AddModelError("", "账号或密码错误");
            return View(vm);
        }
        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "账号或密码错误");
            return View(vm);
        }
        if (user.Status != UserStatus.Approved)
        {
            ModelState.AddModelError("", user.Status == UserStatus.Pending
                ? "账号正在审核中，请耐心等待管理员审核通过"
                : "账号审核未通过，请联系管理员");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = vm.RememberMe });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // ===== 我的资料（校友信息维护） =====
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var profile = await _db.AlumniProfiles.FirstOrDefaultAsync(p => p.UserId == User.GetUserId());
        if (profile == null) return NotFound();
        return View(profile);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(AlumniProfile input)
    {
        var profile = await _db.AlumniProfiles.FirstOrDefaultAsync(p => p.UserId == User.GetUserId());
        if (profile == null || !ModelState.IsValid) return View(profile ?? input);

        profile.StudentNo = input.StudentNo;
        profile.Name = input.Name;
        profile.GradYear = input.GradYear;
        profile.Department = input.Department;
        profile.Major = input.Major;
        profile.Industry = input.Industry;
        profile.City = input.City;
        profile.Company = input.Company;
        profile.Position = input.Position;
        profile.Bio = input.Bio;
        profile.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Success"] = "个人资料已更新";
        return RedirectToAction(nameof(Profile));
    }

    public IActionResult AccessDenied() => View();
}

public class LoginViewModel
{
    [Required(ErrorMessage = "请输入手机号或邮箱")]
    [Display(Name = "手机号 / 邮箱")]
    public string Account { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入密码"), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, MaxLength(20), Display(Name = "手机号")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, Display(Name = "邮箱")]
    public string? Email { get; set; }

    [Required, MaxLength(50), Display(Name = "姓名")]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "密码至少6位"), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "两次输入的密码不一致"), DataType(DataType.Password)]
    [Display(Name = "确认密码")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, Display(Name = "学号")]
    public string StudentNo { get; set; } = string.Empty;

    [Range(1970, 2100, ErrorMessage = "请输入正确的毕业年份"), Display(Name = "毕业年份")]
    public int GradYear { get; set; } = DateTime.Now.Year;

    [Required, Display(Name = "院系")]
    public string Department { get; set; } = string.Empty;

    [Required, Display(Name = "专业")]
    public string Major { get; set; } = string.Empty;

    [Required, Display(Name = "验证码")]
    public string VerifyCode { get; set; } = string.Empty;

    /// <summary>演示模式生成的验证码（部署时应通过短信/邮件下发）</summary>
    public string GeneratedVerifyCode { get; set; } = string.Empty;
}
