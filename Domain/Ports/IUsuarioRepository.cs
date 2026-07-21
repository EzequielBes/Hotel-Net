using CheckInApp.Domain.Entities;

namespace CheckInApp.Domain.Ports;


public interface IUsuarioRepository
{
    Usuario? ObterPorEmail(string email);
    void Adicionar(Usuario usuario);
}
