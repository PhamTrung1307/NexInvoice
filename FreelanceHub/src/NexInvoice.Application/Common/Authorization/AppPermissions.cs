namespace NexInvoice.Application.Common.Authorization;

public static class AppPermissions
{
    public const string ClientView = "client.view";
    public const string ClientCreate = "client.create";
    public const string ClientUpdate = "client.update";
    public const string ClientDelete = "client.delete";
    public const string ProjectView = "project.view";
    public const string ProjectCreate = "project.create";
    public const string ProjectUpdate = "project.update";
    public const string ProjectDelete = "project.delete";
    public const string TaskView = "task.view";
    public const string TaskCreate = "task.create";
    public const string TaskUpdate = "task.update";
    public const string InvoiceView = "invoice.view";
    public const string InvoiceCreate = "invoice.create";
    public const string InvoiceUpdate = "invoice.update";
    public const string PaymentView = "payment.view";
    public const string PaymentCreate = "payment.create";
    public const string PaymentUpdate = "payment.update";
    public const string DashboardView = "dashboard.view";

    public static readonly IReadOnlyCollection<string> All =
    [
        ClientView,
        ClientCreate,
        ClientUpdate,
        ClientDelete,
        ProjectView,
        ProjectCreate,
        ProjectUpdate,
        ProjectDelete,
        TaskView,
        TaskCreate,
        TaskUpdate,
        InvoiceView,
        InvoiceCreate,
        InvoiceUpdate,
        PaymentView,
        PaymentCreate,
        PaymentUpdate,
        DashboardView
    ];
}
