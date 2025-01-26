using APICatalogo.Repository;
using GraphQL;
using GraphQL.Types;

namespace APICatalogo.GraphQL
{
    /// <summary>
    /// Mapeia os campos para uma dada consulta
    /// </summary>
    public class CategoriaQuery : ObjectGraphType
    {
        // O construtor vai receber a instância do UnitOfWork dos repositórios
        public CategoriaQuery(IUnitOfWork _context)
        {
            // O método vai retornar um objeto Categoria
            Field<CategoriaType>("categoria",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType>() { Name = "id" }),
                    resolve: context =>
                    {
                        var id = context.GetArgument<int>("id");
                        return _context.CategoriaRepository
                                       .GetById(c => c.CategoriaId == id);
                    });

            //nosso método vai retornar uma lista de objetos categoria
            // aqui resolve vai mapear a requisição do cliente com os dados 
            //da consulta Get definida em CategoriaRepository
            Field<ListGraphType<CategoriaType>>("categorias",
                resolve: context =>
                {
                    return _context.CategoriaRepository.Get();
                });
        }
    }
}
