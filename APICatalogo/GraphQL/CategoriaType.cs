using APICatalogo.Models;
using GraphQL.Types;

namespace APICatalogo.GraphQL
{
    /// <summary>
    /// Define a entidade que será mapeada para o type
    /// </summary>
    public class CategoriaType : ObjectGraphType<Categoria>
    {
        public CategoriaType()
        {
            // Campos do Type
            Field(x => x.CategoriaId);
            Field(x => x.Nome);
            Field(x => x.ImagemUrl);

            Field<ListGraphType<CategoriaType>>("categorias");
        }
    }
}
