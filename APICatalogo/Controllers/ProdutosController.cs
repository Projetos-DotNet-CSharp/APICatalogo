using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Filters;
using APICatalogo.Models;
using APICatalogo.Pagination;
using APICatalogo.Repository;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace APICatalogo.Controllers
{
    /* ApiConventionType define os tipos de respostas HTTP retornadas (inclusive na documentação do Swagger)*/
    [ApiConventionType(typeof(DefaultApiConventions))] 
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class ProdutosController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public ProdutosController(IUnitOfWork context, IMapper mapper)
        {
            _uow = context;
            _mapper = mapper;
        }

        [HttpGet("menorpreco")]
        public async Task<ActionResult<IEnumerable<ProdutoDTO>>> GetProdutosPrecos()
        {
            var produtos = await _uow.ProdutoRepository.GetProdutosPorPreco();
            var produtosDto = _mapper.Map<List<ProdutoDTO>>(produtos);

            return produtosDto;
        }

        
        /*
         O método .AsNoTracking() no EF não deixa rastrear o objeto para fazer cache. Pois é consulta somente leitura.
         Isso melhora a performance. 
         OBS: Só deve ser usado quando temos certeza que o objeto não precisar ser atualizado posteriormente(update). 
        */
        /// <summary>
        /// Exibe uma relação dos produtos
        /// </summary>
        /// <param name="produtosParameters">Objeto produtos com parâmetros</param>
        /// <returns>Retorna uma lista de objetos Produto</returns>
        [HttpGet]
        [ServiceFilter(typeof(ApiLoggingFilter))]
        public async Task<ActionResult<IEnumerable<ProdutoDTO>>> Get([FromQuery] ProdutosParameters produtosParameters)
        {
            var produtos = await _uow.ProdutoRepository.GetProdutos(produtosParameters);

            if (produtos is null)
            {
                return NotFound("Produtos não encontrados...");
            }

            var metadata = new
            {
                produtos.TotalCount,
                produtos.PageSize,
                produtos.CurrentPage,
                produtos.TotalPages,
                produtos.HasNext,
                produtos.HasPrevious
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(metadata));

            var produtosDto = _mapper.Map<List<ProdutoDTO>>(produtos);

            return produtosDto;
        }

         /*
         Quando existem outros parâmetros que não estão indicados no HttpGet("{}"), estes são querystrings,
         passados após o símbolo '?' e definidos nos atributos do método.
         Caso queira definí-los como obrigatórios, deve-se colocar antes o [BindRequired].        
         O id:int serve pra restringir o tipo de parâmetro a ser passado na url (ex: inteiro).
         O Name = "ObterProduto" serve para criar uma rota, a qual pode ser utilizada pelos métodos deste controller.
         min(1) serve para indicar que o parâmetro tem que ser >= 1 (caso contrário não executa a action).
        */
        
        /// <summary>
        /// Obtém um produto pelo seu identificador ProdutoId
        /// </summary>
        /// <param name="id">Código do Produto</param>
        /// <returns>Um objeto Produto</returns>
        [HttpGet("{id:int:min(1)}", Name = "ObterProduto")] 
        public async Task<ActionResult<ProdutoDTO>> Get(int id)
        {
            // Testando o ConfigureExceptionHandler (exceção customizada) da classe ApiExceptionMiddlewareExtensions
            // throw new Exception("Exception ao retornar produto pelo id");

            var produto = await _uow.ProdutoRepository.GetById(p => p.ProdutoId == id);

            if (produto is null)
            {
                return NotFound("Produto não encontrado...");
            }

            var produtoDto = _mapper.Map<ProdutoDTO>(produto);

            return produtoDto;
        }

        [HttpPost]
        public async Task<ActionResult> Post(ProdutoDTO produtoDto)
        {
            var produto = _mapper.Map<Produto>(produtoDto);

            _uow.ProdutoRepository.Add(produto);
            await _uow.Commit();

            var produtoDtoRetornado = _mapper.Map<ProdutoDTO>(produto);

            // Retorna criando uma rota definida no método Get(int id) [Name = "ObterProduto"].            
            return new CreatedAtRouteResult("ObterProduto", new { id = produto.ProdutoId }, produtoDtoRetornado);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, ProdutoDTO produtoDto)
        {
            if (id != produtoDto.ProdutoId)
            {
                return BadRequest();
            }

            var produto = _mapper.Map<Produto>(produtoDto);

            _uow.ProdutoRepository.Update(produto);
            await _uow.Commit();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ProdutoDTO>> Delete(int id)
        {
            var produto = await _uow.ProdutoRepository.GetById(p => p.ProdutoId == id);

            if (produto is null)
            {
                return NotFound("Produto não localizado...");
            }

            _uow.ProdutoRepository.Delete(produto);
            await _uow.Commit();

            var produtoDto = _mapper.Map<ProdutoDTO>(produto);

            return produtoDto;
        }
    }
}
