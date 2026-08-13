namespace BorealBoost.Core.Foundation;

public enum AdminStatusKind
{
    Active,
    Required
}

public sealed record AdminStatus(AdminStatusKind Kind, string DisplayText)
{
    public static AdminStatus Active() => new(AdminStatusKind.Active, "Administrador: Ativo");

    public static AdminStatus Required() => new(AdminStatusKind.Required, "Administrador: Necessario");
}

public interface IAdminStatusProvider
{
    AdminStatus GetCurrentStatus();
}
