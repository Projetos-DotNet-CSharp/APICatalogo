using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Models;
using APICatalogo.Pagination;
using APICatalogo.Repository;
using APICatalogo.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace APICatalogo.Controllers
{
    
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(AuthenticationSchemes = "Bearer")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CategoriasController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        

        public CategoriasController(IUnitOfWork contexto, IConfiguration config, ILogger<CategoriasController> logger, IMapper mapper)
        {
            _uow = contexto;
            _configuration = config;
            _logger = logger;
            _mapper = mapper;
        }

        //// Construtor usado nos testes (ApiCatalogoxUnitTests)
        //public CategoriasController(IUnitOfWork contexto, IMapper mapper)
        //{
        //    _uow = contexto;
        //    _mapper = mapper;
        //}

        [AllowAnonymous]
        [HttpGet("teste")]
        public string GetTeste()
        {
            return $"CategoriasController - {DateTime.Now.ToLongDateString().ToString()}";
        }

        // Pegar dados de configuração do arquivo appsettings.json (IConfiguration)
        [HttpGet("autor")]
        public string GetAutor()
        {
            var autor = _configuration["autor"];
            var conexao = _configuration["ConnectionStrings:DefaultConnection"];

            return $"Autor : {autor} Conexão : {conexao}";
        }

        // [FromServices] faz a injeção de dependência diretamente na action.
        [HttpGet("saudacao/{nome}")]
        public ActionResult<string> GetSaudacao([FromServices] IMeuServico meuServico, string nome)
        {
            return meuServico.Saudacao(nome);
        }


        #region -> Anotações
        /* Este método usa duas classes que se auto referenciam (agregação). Na serialização do json ocorre um erro
         de referência cíclica. Para eliminar este problema é necessário adicionar a opção "IgnoreCycles" no método
         builder.Services.AddControllers() da classe Program. */
        #endregion

        [HttpGet("produtos")]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategoriasProdutos()
        {
            _logger.LogInformation("======================= GET api/categorias/produtos/controle =======================");

            var categorias = await _uow.CategoriaRepository.GetCategoriasProdutos();
            var categoriasDto = _mapper.Map<List<CategoriaDTO>>(categorias);

            #region -> Anotações
            /* O método Where é usado pra restringir a consulta.Pois é uma boa prática não retornar todos os dados diretamente
            na mesma consulta, pois onera o banco.

            O método .AsNoTracking() no EF não deixa ratrear o objeto para fazer cache. Pois é consulta somente leitura.
            Isso melhora a performance. 
            OBS: Só deve ser usado quando temos certeza que o objeto não precisar ser atualizado posteriormente(update). */
            #endregion
            return categoriasDto;
        }

        [HttpGet("compaginacao")] // Excluir o nome do get 'compaginacao' depois
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> Get([FromQuery] CategoriasParameters categoriasParameters)
        {
            _logger.LogInformation("======================= GET api/categorias/controle =======================");

            //var categorias = _uow.CategoriaRepository.Get().ToList();
            var categorias = await _uow.CategoriaRepository.GetCategorias(categoriasParameters);

            if (categorias is null)
            {
                return NotFound("Categorias não encontradas...");
            }

            var metadata = new
            {
                categorias.TotalCount,
                categorias.PageSize,
                categorias.CurrentPage,
                categorias.TotalPages,
                categorias.HasNext,
                categorias.HasPrevious
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(metadata));

            var categoriasDto = _mapper.Map<List<CategoriaDTO>>(categorias);

            return categoriasDto;
        }

        /* Sem paginação (usado em Teste)*/
        //[HttpGet]
        //public ActionResult<IEnumerable<CategoriaDTO>> Get()
        //{

        //    try
        //    {
        //        var categorias = _uow.CategoriaRepository.Get().ToListAsync();
        //        var categoriasDto = _mapper.Map<List<CategoriaDTO>>(categorias);
        //        //throw new Exception();
        //        return categoriasDto;
        //    }
        //    catch (Exception)
        //    {
        //        return BadRequest();
        //    }
        //}

        /* 
         O id:int serve pra restringir o tipo de parâmetro a ser passado na url.
         O Name = "ObterCategoria" serve para criar uma rota, a qual pode ser utilizada pelos métodos deste controller. 
        */
        /// <summary>
        /// Obtem uma Categoria pelo seu Id
        /// </summary>
        /// <param name="id">código da Categoria</param>
        /// <returns>Objeto Categoria</returns>
        [HttpGet("{id:int}", Name = "ObterCategoria")]
        [ProducesResponseType(typeof(ProdutoDTO), StatusCodes.Status200OK)] /* O Produces response type 200Ok já é padrão, nem precisaria ser adicionado */
        [ProducesResponseType(StatusCodes.Status404NotFound)] 
        public async Task<ActionResult<Categoria>> Get(int id)
        {
            _logger.LogInformation($"======================= GET api/categorias/id = {id} =======================");

            try
            {
                var categoria = await _uow.CategoriaRepository.GetById(p => p.CategoriaId == id);

                if (categoria == null)
                {
                    _logger.LogInformation($"======================= GET api/categorias/id = {id} NOT FOUND =======================");

                    return NotFound($"Categoria com id = {id} não encontrada...");
                }

                var categoriaDto = _mapper.Map<CategoriaDTO>(categoria);

                return Ok(categoriaDto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                                  "Ocorreu um problema ao tratar a sua solicitação.");
            }
        }


        /*
         Retorna criando uma rota definida no método Get(int id).
         É necessário definir essa rota no Get(int id) para funcionar. 
        */
        /// <summary>
        /// Inclui uma nova categoria
        /// </summary>
        /// <remarks>
        /// Exemplo de request:
        /// 
        ///  POST api/categorias
        ///  {
        ///     "categoriaId": 1,
        ///     "nome": "categoria1",
        ///     "imagemUrl": "http://teste.net/1.jpg"
        ///  }
        /// </remarks>
        /// <param name="categoriaDto">objeto Categoria</param>
        /// <returns>O objeto Categoria incluída</returns>
        /// <remarks>Retorna um objeto Categoria incluído</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Post(CategoriaDTO categoriaDto)
        {
            var categoria = _mapper.Map<Categoria>(categoriaDto);

            _uow.CategoriaRepository.Add(categoria);
            await _uow.Commit();

            var categoriaDtoRetornada = _mapper.Map<CategoriaDTO>(categoria);

            // Retorna criando uma rota definida no método Get(int id) [Name = "ObterCategoria"].                   
            return new CreatedAtRouteResult("ObterCategoria", new { id = categoria.CategoriaId }, categoriaDtoRetornada);
        }

        /* ApiConventionMethod define os tipos de respostas HTTP retornadas no Swagger */
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, CategoriaDTO categoriaDto)
        {
            if (id != categoriaDto.CategoriaId)
            {
                return BadRequest();
            }

            var categoria = _mapper.Map<Categoria>(categoriaDto);

            _uow.CategoriaRepository.Update(categoria);
            await _uow.Commit();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<CategoriaDTO>> Delete(int id)
        {
            var categoria = await _uow.CategoriaRepository.GetById(p => p.CategoriaId == id);

            if (categoria == null)
            {
                return NotFound($"Categoria com id = {id} não encontrada...");
            }

            _uow.CategoriaRepository.Delete(categoria);
            await _uow.Commit();

            var categoriaDto = _mapper.Map<CategoriaDTO>(categoria);

            return categoriaDto;
        }
    }
}
