using Microsoft.AspNetCore.Mvc;
using NotasFiscais.Api.Dtos;
using NotasFiscais.Api.Services;

namespace NotasFiscais.Api.Controllers;

[ApiController]
[Route("api/notas")]
public class NotasController(INotaFiscalService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotaFiscalResponse>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<NotaFiscalResponse>> Listar() => Ok(service.Listar());

    [HttpGet("{numero}")]
    [ProducesResponseType(typeof(NotaFiscalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<NotaFiscalResponse> ObterPorNumero(string numero) =>
        service.ObterPorNumero(numero) is { } nota ? Ok(nota) : NotFound();

    [HttpPost]
    [ProducesResponseType(typeof(NotaFiscalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<NotaFiscalResponse> Cadastrar(CriarNotaRequest request)
    {
        var resultado = service.Cadastrar(request);

        switch (resultado.Status)
        {
            case StatusCadastro.Criada:
                return CreatedAtAction(nameof(ObterPorNumero), new { numero = resultado.Nota!.Numero }, resultado.Nota);

            case StatusCadastro.DataEmissaoFutura:
                ModelState.AddModelError(nameof(request.DataEmissao), "A data de emissão não pode ser futura.");
                return ValidationProblem(ModelState);

            default:
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Nota fiscal já cadastrada.",
                    Detail = $"Já existe uma nota com o número {request.Numero}."
                });
        }
    }
}
