using APICatalogo.Models;
using APICatalogo.Pagination;

namespace APICatalogo.Repository
{
    public interface ICategoriaRepository : IRepository<Categoria>
    {
        //PagedList<Categoria> GetCategorias(CategoriasParameters categoriasParameters);
        Task<PagedList<Categoria>> GetCategorias(CategoriasParameters categoriasParameters);

        Task<IEnumerable<Categoria>> GetCategoriasProdutos();
    }
}
