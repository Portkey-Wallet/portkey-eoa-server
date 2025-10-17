using Volo.Abp.Domain.Entities;

namespace EoaServer.Entities.Es;

public abstract class EoaBaseEsEntity<TKey> : Entity, IEntity<TKey>
{
    public virtual TKey Id { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }
}