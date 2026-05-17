using NexInvoice.Domain.Common;

namespace NexInvoice.Application.Interfaces;

public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
}
