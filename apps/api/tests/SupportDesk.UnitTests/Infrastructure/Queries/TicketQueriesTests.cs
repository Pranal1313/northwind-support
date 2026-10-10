using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

/// <summary>
/// Exercises the real <see cref="TicketQueries"/> against SQLite, so filters run through EF
/// rather than a mock of <c>ITicketQueries</c>.
/// </summary>
public sealed class TicketQueriesTests
{
    [Fact]
    public async Task GetPagedAsync_WithoutFilters_ReturnsEveryTicketAndTheFullCount()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(new TicketQuery(), CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(5, result.Items.Select(t => t.Id).Distinct().Count());
    }

    [Fact]
    public async Task GetPagedAsync_ByPriority_ReturnsExactMatches()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(TicketPriority.High, item.Priority));
        Assert.Equal(
            new[] { db.Invoice.Id, db.WrongAmount.Id }.OrderByDescending(id => id),
            result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ByStatus_ReturnsExactMatches()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Status = TicketStatus.Open },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(db.Vpn.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetPagedAsync_ByCategoryId_ReturnsExactMatches()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { CategoryId = db.Billing.Id },
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(db.Billing.Id, item.Category.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ByCustomerId_ReturnsExactMatches()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { CustomerId = db.Northwind.Id },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(db.Northwind.Id, item.Customer.Id));
    }

    [Fact]
    public async Task GetPagedAsync_ByAssignedAgentId_ReturnsExactMatches()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { AssignedAgentId = db.Ada.Id },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(db.Vpn.Id, Assert.Single(result.Items).Id);
        Assert.Equal(db.Ada.Id, result.Items[0].AssignedAgent?.Id);
    }

    [Fact]
    public async Task GetPagedAsync_UnassignedOnly_ReturnsTicketsWithNoAgent()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { UnassignedOnly = true },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Null(item.AssignedAgent));
        Assert.Equal(
            new[] { db.Invoice.Id, db.Website.Id }.OrderByDescending(id => id),
            result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetPagedAsync_Search_MatchesTitle()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "invoice" },
            CancellationToken.None);

        Assert.Equal(db.Invoice.Id, Assert.Single(result.Items).Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_Search_MatchesReference()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "TCK-0003" },
            CancellationToken.None);

        Assert.Equal(db.WrongAmount.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetPagedAsync_Search_MatchesCustomerName()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "contoso" },
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Contoso Ltd", item.Customer.Name));
    }

    [Fact]
    public async Task GetPagedAsync_Search_ORsTitleReferenceAndCustomerName()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "TCK-0004" },
            CancellationToken.None);

        Assert.Equal(db.Website.Id, Assert.Single(result.Items).Id);

        result = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "VPN" },
            CancellationToken.None);

        Assert.Equal(db.Vpn.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetPagedAsync_Search_TrimsAndIgnoresBlank()
    {
        using var db = new TicketQueryDatabase();

        var trimmed = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "  Contoso  " },
            CancellationToken.None);
        var blank = await db.Queries.GetPagedAsync(
            new TicketQuery { Search = "   " },
            CancellationToken.None);

        Assert.Equal(3, trimmed.TotalCount);
        Assert.Equal(5, blank.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_CombinedFilters_AreANDed()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery
            {
                Search = "Contoso",
                Status = TicketStatus.New,
                Priority = TicketPriority.High,
                CategoryId = db.Billing.Id,
                CustomerId = db.Contoso.Id,
                UnassignedOnly = true
            },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(db.Invoice.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetPagedAsync_UnassignedOnlyAndAssignedAgent_ReturnsNothing()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { AssignedAgentId = db.Ada.Id, UnassignedOnly = true },
            CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetPagedAsync_TotalCount_IsTheFilteredSetNotThePage()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High, Page = 1, PageSize = 1 },
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(db.WrongAmount.Id, Assert.Single(result.Items).Id);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetPagedAsync_AfterFiltering_KeepsCreatedAtSortAndIdTieBreak()
    {
        using var db = new TicketQueryDatabase();

        var result = await db.Queries.GetPagedAsync(
            new TicketQuery { CustomerId = db.Contoso.Id, SortBy = "createdAtUtc", SortDirection = "desc" },
            CancellationToken.None);

        Assert.Equal(new[] { db.Printer.Id, db.Vpn.Id, db.Invoice.Id }, result.Items.Select(item => item.Id));
        Assert.Equal(SlaStatus.NotApplicable, result.Items[0].SlaStatus);
    }

    /// <summary>
    /// In-memory SQLite with the production mappings. SQL Server included columns are stripped
    /// so <c>EnsureCreated</c> can run; filtered indexes and <c>datetime2</c> type names are
    /// accepted by SQLite as-is.
    /// </summary>
    private sealed class TicketQueryDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        public TicketQueryDatabase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SupportDbContext>()
                .UseSqlite(_connection)
                .Options;

            Db = new SqliteCompatibleSupportDbContext(options);
            Db.Database.EnsureCreated();

            Queries = new TicketQueries(Db, new FixedClock());
            Seed();
        }

        public SupportDbContext Db { get; }

        public TicketQueries Queries { get; }

        public Category Billing { get; private set; } = null!;

        public Category Outage { get; private set; } = null!;

        public Customer Contoso { get; private set; } = null!;

        public Customer Northwind { get; private set; } = null!;

        public Agent Ada { get; private set; } = null!;

        public Agent Linus { get; private set; } = null!;

        public Ticket Invoice { get; private set; } = null!;

        public Ticket Vpn { get; private set; } = null!;

        public Ticket WrongAmount { get; private set; } = null!;

        public Ticket Website { get; private set; } = null!;

        public Ticket Printer { get; private set; } = null!;

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }

        private void Seed()
        {
            var now = FixedClock.DefaultNow;

            Billing = new Category("Billing", requiresSpecialist: false, forcesCriticalPriority: false);
            Outage = new Category("Outage", requiresSpecialist: true, forcesCriticalPriority: true);
            Contoso = new Customer("Contoso Ltd", "ops@contoso.test", null, CustomerTier.Premium, now);
            Northwind = new Customer("Northwind Traders", "help@northwind.test", null, CustomerTier.Standard, now);
            Ada = new Agent("Ada Lovelace", "ada@support.test", 10, now);
            Linus = new Agent("Linus Torvalds", "linus@support.test", 10, now);

            Db.Set<Category>().AddRange(Billing, Outage);
            Db.Set<Customer>().AddRange(Contoso, Northwind);
            Db.Set<Agent>().AddRange(Ada, Linus);
            Db.SaveChanges();

            Invoice = Raise("TCK-0001", "Cannot print invoice", Contoso.Id, Billing.Id, TicketPriority.High, now);
            Vpn = Raise("TCK-0002", "VPN is down", Contoso.Id, Outage.Id, TicketPriority.Medium, now.AddMinutes(1));
            Vpn.AssignTo(Ada.Id, now.AddMinutes(1));
            Vpn.ChangeStatus(TicketStatus.Open, now.AddMinutes(1));

            WrongAmount = Raise("TCK-0003", "Wrong amount charged", Northwind.Id, Billing.Id, TicketPriority.High, now.AddMinutes(2));
            WrongAmount.AssignTo(Linus.Id, now.AddMinutes(2));
            WrongAmount.ChangeStatus(TicketStatus.InProgress, now.AddMinutes(2));

            Website = Raise("TCK-0004", "Website timeout", Northwind.Id, Outage.Id, TicketPriority.Low, now.AddMinutes(3));

            Printer = Raise("TCK-0005", "Printer jam in accounts", Contoso.Id, Billing.Id, TicketPriority.Low, now.AddMinutes(4));
            Printer.AssignTo(Linus.Id, now.AddMinutes(4));
            Printer.ChangeStatus(TicketStatus.Resolved, now.AddMinutes(4));

            Db.Set<Ticket>().AddRange(Invoice, Vpn, WrongAmount, Website, Printer);
            Db.SaveChanges();
        }

        private static Ticket Raise(
            string reference,
            string title,
            int customerId,
            int categoryId,
            TicketPriority priority,
            DateTime createdAtUtc) =>
            Ticket.Raise(
                reference,
                title,
                "A description long enough to be realistic.",
                customerId,
                categoryId,
                priority,
                createdAtUtc);
    }

    /// <summary>
    /// The production model stores included columns on two indexes (SQL Server INCLUDE).
    /// SQLite has no equivalent, so those annotations are cleared before the schema is created.
    /// </summary>
    private sealed class SqliteCompatibleSupportDbContext(DbContextOptions<SupportDbContext> options)
        : SupportDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var index in modelBuilder.Model.GetEntityTypes().SelectMany(type => type.GetIndexes()))
            {
                index.SetAnnotation("SqlServer:Include", null);
            }
        }
    }
}
