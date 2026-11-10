using AlumniAssociationSystem.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlumniAssociationSystem.Web.Data;

/// <summary>数据库初始化与种子数据（演示数据，首次启动自动写入）</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        // 演示项目直接按模型建库；若改用 SQL Server 并采用 EF 迁移，可替换为 MigrateAsync()
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();

        // 管理员账号：admin / Admin@123（手机号注册登录）
        var admin = new User
        {
            Phone = "13800000000", Email = "admin@alumni.edu.cn", Name = "系统管理员",
            Role = UserRole.Admin, Status = UserStatus.Approved, CreatedAt = DateTime.Now.AddDays(-30)
        };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin@123");

        // 若干已审核校友
        var alumni = new List<User>();
        var seeds = new (string Phone, string Name, string Dept, string Major, int Year, string City, string Company)[]
        {
            ("13900000001", "商杰", "计算机学院", "计算机科学与技术", 2020, "上海", "某互联网公司"),
            ("13900000002", "李畅", "计算机学院", "计算机科学与技术", 2021, "北京", "某金融科技公司"),
            ("13900000003", "李雪松", "机械工程学院", "机械工程", 2019, "深圳", "某智能制造企业"),
            ("13900000004", "蒋韵霖", "经济管理学院", "工商管理", 2022, "杭州", "某电商公司"),
            ("13900000005", "王斌", "外国语学院", "英语", 2020, "广州", "某外贸集团"),
        };
        foreach (var s in seeds)
        {
            var u = new User { Phone = s.Phone, Name = s.Name, Role = UserRole.Alumni, Status = UserStatus.Approved, CreatedAt = DateTime.Now.AddDays(-20) };
            u.PasswordHash = hasher.HashPassword(u, "Alumni@123");
            alumni.Add(u);
        }
        db.Users.AddRange(new[] { admin }.Concat(alumni));
        await db.SaveChangesAsync();

        db.AlumniProfiles.AddRange(alumni.Select((u, i) => new AlumniProfile
        {
            UserId = u.Id, StudentNo = $"2016010{i + 1:000}", Name = u.Name,
            GradYear = new[] { 2020, 2021, 2019, 2022, 2020 }[i],
            Department = new[] { "计算机学院", "计算机学院", "机械工程学院", "经济管理学院", "外国语学院" }[i],
            Major = new[] { "计算机科学与技术", "计算机科学与技术", "机械工程", "工商管理", "英语" }[i],
            Industry = new[] { "互联网", "金融科技", "智能制造", "电子商务", "外贸" }[i],
            City = new[] { "上海", "北京", "深圳", "杭州", "广州" }[i],
            Company = new[] { "某互联网公司", "某金融科技公司", "某智能制造企业", "某电商公司", "某外贸集团" }[i],
            Position = new[] { "软件工程师", "开发工程师", "设备工程师", "运营专员", "外贸经理" }[i],
            Bio = "热爱母校，愿为母校发展贡献力量。"
        }));
        await db.SaveChangesAsync();

        // 待审核校友（演示审核流程）
        var pending = new User { Phone = "13700000001", Name = "张小明", Status = UserStatus.Pending, CreatedAt = DateTime.Now.AddDays(-1) };
        pending.PasswordHash = hasher.HashPassword(pending, "Alumni@123");
        db.Users.Add(pending);
        await db.SaveChangesAsync();
        db.AlumniProfiles.Add(new AlumniProfile { UserId = pending.Id, StudentNo = "2024010999", Name = "张小明", GradYear = 2024, Department = "计算机学院", Major = "软件工程" });
        await db.SaveChangesAsync();

        // 活动种子
        var ev = new Event
        {
            Title = "2026年校友返校日暨校友年会",
            Description = "母校喜迎八方校友！活动包含校史参观、院系座谈、晚宴联谊等环节，欢迎大家踊跃报名。",
            Location = "学校大礼堂", StartTime = DateTime.Now.AddDays(15).AddHours(9), EndTime = DateTime.Now.AddDays(15).AddHours(17),
            Capacity = 300, Deadline = DateTime.Now.AddDays(10), Status = EventStatus.Published, CreatedById = admin.Id
        };
        var ev2 = new Event
        {
            Title = "校友职业发展分享会（计算机专场）",
            Description = "邀请多位互联网行业校友分享职业成长经验，并有现场答疑与内推机会。",
            Location = "计算机学院报告厅", StartTime = DateTime.Now.AddDays(7).AddHours(14), EndTime = DateTime.Now.AddDays(7).AddHours(16),
            Capacity = 80, Deadline = DateTime.Now.AddDays(5), Status = EventStatus.Published, CreatedById = admin.Id
        };
        var ev3 = new Event
        {
            Title = "秋季校友羽毛球友谊赛",
            Description = "以球会友，强身健体。设男单、女单、混双三个组别。",
            Location = "学校体育馆", StartTime = DateTime.Now.AddDays(3).AddHours(9), EndTime = DateTime.Now.AddDays(3).AddHours(12),
            Capacity = 60, Deadline = DateTime.Now.AddDays(2), Status = EventStatus.PendingApproval, CreatedById = admin.Id
        };
        db.Events.AddRange(ev, ev2, ev3);
        await db.SaveChangesAsync();

        db.EventRegistrations.Add(new EventRegistration
        { EventId = ev2.Id, UserId = alumni[0].Id, Status = RegistrationStatus.Registered, ContactPhone = alumni[0].Phone });

        // 捐赠项目种子
        var p1 = new DonationProject { Title = "图书馆改扩建基金", Description = "助力母校图书馆数字化改造，为学弟学妹营造更好的学习环境。", TargetAmount = 500000, Status = ProjectStatus.Active, CreatedAt = DateTime.Now.AddDays(-60) };
        var p2 = new DonationProject { Title = "贫困学子助学金", Description = "定向资助家庭经济困难的在校生完成学业。", TargetAmount = 200000, Status = ProjectStatus.Active, CreatedAt = DateTime.Now.AddDays(-30) };
        var p3 = new DonationProject { Title = "校史馆建设基金", Description = "该项目已完成募集，感谢各位校友的支持！", TargetAmount = 100000, Status = ProjectStatus.Closed, CreatedAt = DateTime.Now.AddDays(-300) };
        db.DonationProjects.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        db.DonationRecords.AddRange(
            new DonationRecord { ProjectId = p1.Id, UserId = alumni[1].Id, DonorName = "李畅", Amount = 2000, IsPaid = true, Message = "祝母校越来越好！", CreatedAt = DateTime.Now.AddDays(-10) },
            new DonationRecord { ProjectId = p1.Id, UserId = alumni[2].Id, DonorName = "李雪松", Amount = 500, IsPaid = true, IsAnonymous = true, CreatedAt = DateTime.Now.AddDays(-5) },
            new DonationRecord { ProjectId = p2.Id, UserId = alumni[3].Id, DonorName = "蒋韵霖", Amount = 1000, IsPaid = true, Message = "希望学弟学妹们学业有成", CreatedAt = DateTime.Now.AddDays(-3) },
            new DonationRecord { ProjectId = p3.Id, UserId = alumni[4].Id, DonorName = "王斌", Amount = 3000, IsPaid = true, CreatedAt = DateTime.Now.AddDays(-90) }
        );

        // 资讯种子
        db.NewsArticles.AddRange(
            new NewsArticle { Title = "校友会官方网站正式上线公告", Content = "各位校友：校友会系统今日正式上线，支持活动报名、在线捐赠、招聘信息发布等功能，欢迎体验！", Category = NewsCategory.Notice, Status = PublishStatus.Published, IsPinned = true, AuthorId = admin.Id, PublishTime = DateTime.Now.AddDays(-7) },
            new NewsArticle { Title = "关于举办2026年校友返校日的通知", Content = "经校友会理事会研究决定，2026年校友返校日定于下月举行，具体安排请关注活动页面。", Category = NewsCategory.Notice, Status = PublishStatus.Published, AuthorId = admin.Id, PublishTime = DateTime.Now.AddDays(-3) },
            new NewsArticle { Title = "优秀校友风采：从校园创客到独角兽创始人", Content = "本期校友风采带你走近2008届校友张先生，听他讲述创业路上的坚持与收获……", Category = NewsCategory.Dynamic, Status = PublishStatus.Published, AuthorId = admin.Id, PublishTime = DateTime.Now.AddDays(-5), ViewCount = 128, LikeCount = 36 },
            new NewsArticle { Title = "校友投稿：我的母校记忆", Content = "又是一年梧桐叶落时，想起当年在图书馆自习的日日夜夜……", Category = NewsCategory.Dynamic, Status = PublishStatus.Pending, AuthorId = alumni[0].Id, CreatedAt = DateTime.Now.AddDays(-1) }
        );

        // 招聘种子
        var job1 = new JobPosting { CompanyName = "某互联网公司", Title = "高级后端开发工程师", Responsibilities = "负责核心业务系统的设计与开发；参与技术方案评审与代码审查。", Requirements = "本科及以上学历，3年以上.NET开发经验；熟悉ASP.NET Core、EF Core；良好的沟通协作能力。", Salary = "25K-40K", Location = "上海", Contact = "hr1@example.com", Status = JobStatus.Approved, PostedById = alumni[0].Id, PublishTime = DateTime.Now.AddDays(-4) };
        var job2 = new JobPosting { CompanyName = "某金融科技公司", Title = "数据分析师", Responsibilities = "负责业务数据的采集、清洗与分析，输出分析报告。", Requirements = "统计学/计算机相关专业；熟练使用SQL与Python；有金融行业经验者优先。", Salary = "18K-30K", Location = "北京", Contact = "hr2@example.com", Status = JobStatus.Approved, PostedById = alumni[1].Id, PublishTime = DateTime.Now.AddDays(-2) };
        var job3 = new JobPosting { CompanyName = "某初创科技公司", Title = "前端实习生", Responsibilities = "参与公司产品前端页面开发与维护。", Requirements = "在校本科生/研究生，熟悉 HTML/CSS/JavaScript。", Salary = "3K-5K", Location = "深圳", Contact = "hr3@example.com", Status = JobStatus.Pending, PostedById = alumni[2].Id };
        db.JobPostings.AddRange(job1, job2, job3);
        await db.SaveChangesAsync();

        db.JobApplications.Add(new JobApplication { JobPostingId = job1.Id, UserId = alumni[4].Id, ResumeSummary = "5年.NET后端开发经验，主导过大型电商系统重构。", Status = ApplicationStatus.Reviewed });

        // 站内信种子
        db.Messages.AddRange(
            new Message { RecipientId = alumni[0].Id, Title = "欢迎加入校友会", Content = "亲爱的商杰校友，欢迎加入校友大家庭！可在“我的资料”中完善个人信息。", IsRead = false },
            new Message { RecipientId = alumni[1].Id, Title = "活动报名成功提醒", Content = "您已成功报名“2026年校友返校日暨校友年会”，请准时参加。", IsRead = true },
            new Message { RecipientId = alumni[2].Id, Title = "捐赠证书领取通知", Content = "感谢您对“图书馆改扩建基金”的支持，捐赠证书可在个人中心查看。", IsRead = false }
        );

        // 操作日志种子
        db.OperationLogs.Add(new OperationLog { OperatorId = admin.Id, OperatorName = "系统管理员", Action = "初始化系统", Detail = "数据库初始化完成，写入演示数据", Ip = "127.0.0.1" });

        await db.SaveChangesAsync();
    }
}
