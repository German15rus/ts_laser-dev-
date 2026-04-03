namespace TsLaser.Crm.Api.Infrastructure.Security;

public interface IPasswordService
{
    bool Verify(string password);
}
